FROM ollama/ollama:latest

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