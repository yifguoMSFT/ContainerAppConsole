docker build -t jeffgyf/consoleapi .
docker push jeffgyf/consoleapi
k delete daemonset pod-analyzer-daemon
k apply -f .\KubernetesConfigs\previleged-daemonset.yaml