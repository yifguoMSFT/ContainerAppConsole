docker build -t jeffgyf/consoleapi .
docker push jeffgyf/consoleapi
k delete daemonset --namespace pod-analyzer pod-analyzer-daemon
k apply -f .\KubernetesConfigs\previleged-daemonset.yaml