# syntax=docker/dockerfile:1.4
FROM nvidia/cuda:11.8.0-runtime-ubuntu22.04
COPY images /home/radiance


USER root
RUN <<EOT bash

    # Update repos
    apt update

    # Set up ssh
    apt install openssh-server sudo -y 
    echo 'root:radiance' | chpasswd 
    sed -i 's/#PermitRootLogin prohibit-password/PermitRootLogin yes/g' /etc/ssh/sshd_config
    sed -i 's/#PermitUserEnvironment no/PermitUserEnvironment yes/g' /etc/ssh/sshd_config
    service ssh start

    # Install libs
    apt install csh libqt5gui5 imagemagick -y

    # Install randiance
    tar -xzf /home/radiance/radiance-*.tar.gz --strip-components=1  -C / 

    # Install OpenImageDenoiser
    mkdir /usr/local/OpenImageDenoiser
    tar -xzf /home/radiance/oidn-*.tar.gz -C /usr/local/OpenImageDenoiser
    mv -v /usr/local/OpenImageDenoiser/oidn-*/* /usr/local/OpenImageDenoiser
    rm -r /usr/local/OpenImageDenoiser/oidn-* 

    # Clean up
    rm -r /home/radiance/*
EOT

# Radiance
ENV PATH $PATH:/usr/local/radiance/bin
ENV RAYPATH $RAYPATH:/usr/local/radiance/lib

# OpenImageDenoiser
ENV PATH /usr/local/OpenImageDenoiser/bin:$PATH

# # IF USING ACCELERAD ALSO COPY BELOW
# ENV PATH /usr/local/accelerad/bin:$PATH 
# ENV RAYPATH /usr/local/accelerad/lib:$RAYPATH 
# ENV LD_LIBRARY_PATH /usr/local/accelerad/bin:$LD_LIBRARY_PATH


EXPOSE 22

# USER radiance
WORKDIR /home/radiance
VOLUME /home/radiance


LABEL org.opencontainers.image.authors="Link Arkitektur"
LABEL org.opencontainers.image.source="https://github.com/linkarkitektur/radiance"
LABEL org.opencontainers.image.description="Radiance"
LABEL version="0.0.1"

CMD env | grep "PATH\|RAYPATH" >> /etc/environment && /usr/sbin/sshd -D