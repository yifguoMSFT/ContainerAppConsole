docker build -t jeffgyf/consoleapi .
docker push jeffgyf/consoleapi
k scale --replicas=0 deployment simple-server
k scale --replicas=1 deployment simple-server