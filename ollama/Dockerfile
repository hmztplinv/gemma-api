FROM ollama/ollama:latest

# Modeli preload et
RUN ollama serve & sleep 5 && ollama pull gemma3:4b

# RUN ollama pull gemma3:4b
# Ollama'nın doğru arayüzde dinlemesini sağlayın
ENV OLLAMA_HOST=0.0.0.0:11434

# Başlangıç betiği oluşturun
RUN echo '#!/bin/sh\n\
# Cloud Run ortamını bilgilendir\n\
echo "Ollama başlatılıyor: port $PORT"\n\
# Ollama servisini başlat ve ön planda çalıştır\n\
exec ollama serve' > /start.sh && \
chmod +x /start.sh

# Başlangıç komutunu belirleyin
ENTRYPOINT ["/start.sh"]