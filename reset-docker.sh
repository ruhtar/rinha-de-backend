#!/bin/bash

# Parar e remover todos os containers
docker stop $(docker ps -a -q)
docker rm $(docker ps -a -q)

# Remover todas as imagens
docker rmi $(docker images -q)

# Remover todos os volumes
docker volume rm $(docker volume ls -q)

# Executar docker-compose up -d
docker-compose up -d
