FROM buildpack-deps:groovy-scm

RUN apt-get update && \
    apt-get install wget && \
    wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get install apt-transport-https && \
    apt-get update && \
    DEBIAN_FRONTEND=noninteractive apt-get install -y dotnet-sdk-2.1

ENTRYPOINT ["/usr/bin/bash", "-c"]
