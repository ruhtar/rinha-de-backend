#!/bin/bash

# Obtém o diretório atual do script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Pausa todos os containers Docker em execução
docker pause $(docker ps -q)

# Deleta todos os containers Docker em execução
docker rm -f $(docker ps -aq)

# Deleta todas as imagens Docker
docker rmi -f $(docker images -q)

# Deleta todos os volumes do Docker
docker volume rm $(docker volume ls -q)

# Força a recriação dos containers
docker-compose -f "${SCRIPT_DIR}/docker-compose.yml" down
docker-compose -f "${SCRIPT_DIR}/docker-compose.yml" up -d --force-recreate

echo "Docker Compose concluído com sucesso!"

