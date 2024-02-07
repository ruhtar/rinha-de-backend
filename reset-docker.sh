#!/bin/bash

# Obtém o diretório atual do script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Força a recriação dos containers
docker-compose -f "${SCRIPT_DIR}/docker-compose.yml" down
docker-compose -f "${SCRIPT_DIR}/docker-compose.yml" up -d --force-recreate

# Deleta todos os volumes do Docker
docker volume rm $(docker volume ls -q)

echo "Docker Compose concluído com sucesso!"

