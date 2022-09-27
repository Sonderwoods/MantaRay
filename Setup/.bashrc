export PATH=$PATH:./usr/local/radiance/bin
export RAYPATH=.:/usr/local/radiance/lib
export LIBGL_ALWAYS_INDIRECT=1 #GWSL
export DISPLAY=$(cat /etc/resolv.conf | grep nameserver | awk '{print $2; exit;}'):0.0 #GWSL
export PULSE_SERVER=tcp:$(cat /etc/resolv.conf | grep nameserver | awk '{print $2;exit;}') #GWSL
