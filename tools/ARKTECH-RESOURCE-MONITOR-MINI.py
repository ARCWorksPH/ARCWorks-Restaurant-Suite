## ARKTECH-RESOURCE MONITOR MINI (Smooth)
## pip install psutil wmi pandas matplotlib
## START =   python .\ARKTECH-RESOURCE-MONITOR-MINI.py

import tkinter as tk
from tkinter import filedialog, messagebox
import psutil
import csv
import time
import threading
import os
from datetime import datetime
import pandas as pd
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

# ---------- Optional GPU support ----------
HAS_WMI = False
w_obj = None
try:
    import wmi
    w_obj = wmi.WMI(namespace="root\\CIMV2")
    HAS_WMI = True
except Exception:
    HAS_WMI = False


def get_gpu_metrics():
    """Heavy query – only call when needed."""
    if not HAS_WMI or w_obj is None:
        return None, None

    gpu_util = None
    vram_used = None

    try:
        counters = w_obj.Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine()
        utils = []
        for c in counters:
            val = getattr(c, "UtilizationPercentage", None)
            if val is not None:
                try:
                    utils.append(int(val))
                except Exception:
                    pass
        if utils:
            gpu_util = max(utils)
    except Exception:
        pass

    try:
        mem_counters = w_obj.Win32_PerfFormattedData_GPUPerformanceCounters_GPUProcessMemory()
        dedicated = 0
        for c in mem_counters:
            val = getattr(c, "DedicatedUsage", None)
            if val is not None:
                try:
                    dedicated += int(val)
                except Exception:
                    pass
        if dedicated > 0:
            vram_used = round(dedicated / (1024 ** 2), 1)
    except Exception:
        pass

    return gpu_util, vram_used


def get_ollama_ram():
    total = 0
    for p in psutil.process_iter(["name", "memory_info"]):
        try:
            name = (p.info["name"] or "").lower()
            if "ollama" in name:
                total += p.info["memory_info"].rss
        except (psutil.NoSuchProcess, psutil.AccessDenied):
            continue
    return round(total / (1024 ** 2), 1) if total else 0


class UniversalResourceLogger:
    def __init__(self, root):
        self.root = root
        self.root.title("ARKTECH · LLM Resource Monitor")
        self.root.geometry("400x280")
        self.root.configure(bg="#1e1e1e")
        self.root.resizable(False, False)

        self.is_logging = False
        self.log_thread = None
        self.log_data = []
        self.start_time = None
        self.enable_gpu = tk.BooleanVar(value=False)   # OFF by default → smooth

        # Header
        tk.Label(root, text="LLM RESOURCE MONITOR", font=("Segoe UI", 14, "bold"),
                 fg="#f1c40f", bg="#1e1e1e").pack(pady=(12, 2))

        self.status = tk.Label(root, text="Ready", font=("Segoe UI", 10),
                               fg="#3498db", bg="#1e1e1e")
        self.status.pack()

        # Live metrics (CPU + RAM only – always fast)
        metrics_frame = tk.Frame(root, bg="#252525", padx=10, pady=8)
        metrics_frame.pack(fill="x", padx=16, pady=8)

        self.lbl_cpu = self._metric(metrics_frame, "CPU", "0 %")
        self.lbl_ram = self._metric(metrics_frame, "RAM", "0 %")
        self.lbl_ollama = self._metric(metrics_frame, "Ollama", "0 MB")

        # GPU toggle
        gpu_frame = tk.Frame(root, bg="#1e1e1e")
        gpu_frame.pack(pady=4)
        tk.Checkbutton(gpu_frame, text="Enable GPU monitoring (slower)",
                       variable=self.enable_gpu, bg="#1e1e1e", fg="#ccc",
                       selectcolor="#333", activebackground="#1e1e1e",
                       font=("Segoe UI", 9)).pack()

        # Buttons
        btn_frame = tk.Frame(root, bg="#1e1e1e")
        btn_frame.pack(pady=10)

        self.btn_start = tk.Button(btn_frame, text="▶  Start Logging", command=self.start_logging,
                                   bg="#27ae60", fg="white", font=("Segoe UI", 10, "bold"),
                                   width=15, relief="flat", padx=4, pady=4)
        self.btn_start.grid(row=0, column=0, padx=6)

        self.btn_stop = tk.Button(btn_frame, text="■  Stop & Report", command=self.stop_logging,
                                  bg="#c0392b", fg="white", font=("Segoe UI", 10, "bold"),
                                  width=15, relief="flat", padx=4, pady=4, state=tk.DISABLED)
        self.btn_stop.grid(row=0, column=1, padx=6)

        tk.Label(root, text="GPU monitoring is optional · Leave it off for best speed",
                 font=("Segoe UI", 8), fg="#666", bg="#1e1e1e").pack(side="bottom", pady=6)

        self.update_live_metrics()

    def _metric(self, parent, title, value):
        f = tk.Frame(parent, bg="#252525")
        f.pack(side="left", expand=True)
        tk.Label(f, text=title, font=("Segoe UI", 8), fg="#aaa", bg="#252525").pack()
        lbl = tk.Label(f, text=value, font=("Segoe UI", 11, "bold"), fg="white", bg="#252525")
        lbl.pack()
        return lbl

    def update_live_metrics(self):
        """Only cheap metrics – this stays smooth."""
        try:
            cpu = psutil.cpu_percent(interval=None)
            ram = psutil.virtual_memory()
            ollama_mb = get_ollama_ram()

            self.lbl_cpu.config(text=f"{cpu:.0f} %")
            self.lbl_ram.config(text=f"{ram.percent:.0f} %")
            self.lbl_ollama.config(text=f"{ollama_mb:.0f} MB")
        except Exception:
            pass
        self.root.after(1000, self.update_live_metrics)

    def start_logging(self):
        self.is_logging = True
        self.start_time = time.time()
        self.log_data = []
        self.btn_start.config(state=tk.DISABLED)
        self.btn_stop.config(state=tk.NORMAL)
        self.status.config(text="Logging…", fg="#2ecc71")

        self.log_thread = threading.Thread(target=self.log_metrics, daemon=True)
        self.log_thread.start()

    def stop_logging(self):
        self.is_logging = False
        self.btn_start.config(state=tk.NORMAL)
        self.btn_stop.config(state=tk.DISABLED)
        self.status.config(text="Saving report…", fg="#f39c12")

        if self.log_thread:
            self.log_thread.join(timeout=2)

        self.save_and_plot()

    def log_metrics(self):
        header = ["Timestamp", "Elapsed_s", "CPU_%", "RAM_%", "RAM_Used_GB",
                  "GPU_%", "VRAM_MB", "Ollama_RSS_MB"]
        self.log_data.append(header)

        last_gpu_time = 0
        gpu_util = None
        vram = None

        while self.is_logging:
            ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            elapsed = round(time.time() - self.start_time, 1)
            cpu = psutil.cpu_percent(interval=None)
            ram = psutil.virtual_memory()
            ollama_mb = get_ollama_ram()

            # Only query GPU every 5 seconds and only if enabled
            if self.enable_gpu.get() and (time.time() - last_gpu_time > 5):
                gpu_util, vram = get_gpu_metrics()
                last_gpu_time = time.time()

            row = [
                ts, elapsed,
                cpu, ram.percent, round(ram.used / (1024**3), 2),
                gpu_util if gpu_util is not None else "",
                vram if vram is not None else "",
                ollama_mb
            ]
            self.log_data.append(row)
            time.sleep(1)

    def save_and_plot(self):
        if len(self.log_data) < 3:
            messagebox.showwarning("No data", "Not enough samples.")
            self.status.config(text="Ready", fg="#3498db")
            return

        file_path = filedialog.asksaveasfilename(
            defaultextension=".csv",
            filetypes=[("CSV files", "*.csv")],
            title="Save Benchmark CSV"
        )
        if not file_path:
            self.status.config(text="Cancelled", fg="#3498db")
            return

        try:
            with open(file_path, "w", newline="", encoding="utf-8") as f:
                csv.writer(f).writerows(self.log_data)

            chart_path = os.path.splitext(file_path)[0] + "_report.png"
            self.generate_report(file_path, chart_path)

            self.status.config(text="Done!", fg="#2ecc71")
            messagebox.showinfo("Success", f"Saved:\n{file_path}")
        except Exception as e:
            self.status.config(text="Error", fg="#e74c3c")
            messagebox.showerror("Error", str(e))

    def generate_report(self, csv_path, png_path):
        df = pd.read_csv(csv_path)
        for col in ["CPU_%", "RAM_%", "GPU_%", "VRAM_MB", "Ollama_RSS_MB"]:
            if col in df.columns:
                df[col] = pd.to_numeric(df[col], errors="coerce")

        fig, axes = plt.subplots(2, 2, figsize=(11, 7.5), facecolor="#1c1c1c")
        fig.suptitle("LLM Benchmark Resource Report", color="white", fontsize=15, fontweight="bold")

        ax1 = axes[0, 0]
        ax1.set_facecolor("#252525")
        ax1.plot(df["Elapsed_s"], df["CPU_%"], label="CPU", color="#3498db", lw=1.6)
        ax1.plot(df["Elapsed_s"], df["RAM_%"], label="RAM", color="#2ecc71", lw=1.6)
        if df["GPU_%"].notna().any():
            ax1.plot(df["Elapsed_s"], df["GPU_%"], label="GPU", color="#f1c40f", lw=1.6)
        ax1.set_ylabel("%", color="white")
        ax1.tick_params(colors="white")
        ax1.legend(facecolor="#333", labelcolor="white", fontsize=8)
        ax1.set_title("Utilization", color="white")
        ax1.grid(True, alpha=0.15)

        ax2 = axes[0, 1]
        ax2.set_facecolor("#252525")
        if df["VRAM_MB"].notna().any():
            ax2.plot(df["Elapsed_s"], df["VRAM_MB"], label="VRAM (MB)", color="#e74c3c", lw=1.6)
        ax2.plot(df["Elapsed_s"], df["Ollama_RSS_MB"], label="Ollama RSS", color="#9b59b6", lw=1.6)
        ax2.set_ylabel("MB", color="white")
        ax2.tick_params(colors="white")
        ax2.legend(facecolor="#333", labelcolor="white", fontsize=8)
        ax2.set_title("Memory", color="white")
        ax2.grid(True, alpha=0.15)

        ax3 = axes[1, 0]
        ax3.set_facecolor("#252525")
        avgs = {
            "CPU": df["CPU_%"].mean(),
            "RAM": df["RAM_%"].mean(),
            "GPU": df["GPU_%"].mean() if df["GPU_%"].notna().any() else 0
        }
        bars = ax3.barh(list(avgs.keys()), list(avgs.values()),
                        color=["#3498db", "#2ecc71", "#f1c40f"])
        ax3.set_xlim(0, 100)
        ax3.tick_params(colors="white")
        ax3.set_title("Average %", color="white")
        for b in bars:
            ax3.text(b.get_width() + 1, b.get_y() + b.get_height()/2,
                     f"{b.get_width():.1f}%", va="center", color="white", fontsize=9)

        ax4 = axes[1, 1]
        ax4.set_facecolor("#252525")
        ax4.axis("off")
        dur = df["Elapsed_s"].max()
        txt = (f"Duration        : {dur:.1f}s\n"
               f"Samples         : {len(df)}\n\n"
               f"CPU  avg/peak   : {df['CPU_%'].mean():.1f}% / {df['CPU_%'].max():.1f}%\n"
               f"RAM  avg/peak   : {df['RAM_%'].mean():.1f}% / {df['RAM_%'].max():.1f}%\n")
        if df["GPU_%"].notna().any():
            txt += f"GPU  avg/peak   : {df['GPU_%'].mean():.1f}% / {df['GPU_%'].max():.1f}%\n"
        if df["VRAM_MB"].notna().any():
            txt += f"VRAM avg/peak   : {df['VRAM_MB'].mean():.0f} / {df['VRAM_MB'].max():.0f} MB\n"
        txt += f"Ollama peak RSS : {df['Ollama_RSS_MB'].max():.0f} MB"
        ax4.text(0.05, 0.95, txt, transform=ax4.transAxes, fontsize=10,
                 va="top", fontfamily="Consolas", color="white")

        plt.tight_layout(rect=[0, 0, 1, 0.95])
        plt.savefig(png_path, dpi=140, facecolor=fig.get_facecolor(), bbox_inches="tight")
        plt.close()


if __name__ == "__main__":
    root = tk.Tk()
    app = UniversalResourceLogger(root)

    def on_closing():
        if app.is_logging:
            app.is_logging = False
            if app.log_thread and app.log_thread.is_alive():
                app.log_thread.join(timeout=1.5)
        root.destroy()

    root.protocol("WM_DELETE_WINDOW", on_closing)
    root.mainloop()