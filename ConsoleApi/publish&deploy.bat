docker build -t jeffgyf/consoleapi .
docker push jeffgyf/consoleapi
k scale --replicas=0 deployment pod-analyzer
k scale --replicas=1 deployment pod-analyzer