Agent Model Benchmark


======tinyllama:1.1b ========
total duration:       19.181252657s
load duration:        54.659942ms
prompt eval count:    891 token(s)
prompt eval duration: 2.807268s
prompt eval rate:     317.39 tokens/s
eval count:           513 token(s)
eval duration:        16.29481s
eval rate:            31.48 tokens/s
=========================

====qwen2.5-coder:7b ========
total duration:       1m48.965954011s
load duration:        241.67409ms
prompt eval count:    40 token(s)
prompt eval duration: 1.188006s
prompt eval rate:     33.67 tokens/s
eval count:           585 token(s)
eval duration:        1m47.532983s
eval rate:            5.44 tokens/s
=========================

docker exec -it arcworks-resto-ollama-1 ollama pull qwen2.5-coder:7b
docker exec -it arcworks-resto-ollama-1 ollama list
docker exec -it arcworks-resto-ollama-1 ollama run qwen2.5-coder:7b
Write a long essay on why the sky is blue.
/set verbose
