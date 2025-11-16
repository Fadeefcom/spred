#!/bin/bash

for i in $(seq 1 60); do
    curl -fsk https://azurecosmosemulator.domain:8081/_explorer/emulator.pem > ~/emulatorcert.crt
    if [ $? -eq 0 ]; then
        cp ~/emulatorcert.crt /usr/local/share/ca-certificates/
        sudo update-ca-certificates
        echo "Cosmos emulator ready"
        $1 $2
        break
    else
        echo "Cosmos Not ready yet..."
        sleep 10
    fi
done