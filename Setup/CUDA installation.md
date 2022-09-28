https://docs.nvidia.com/cuda/wsl-user-guide/index.html#getting-started-with-cuda-on-wsl
didnt really solve it

what about:
https://ubuntu.com/blog/getting-started-with-cuda-on-ubuntu-on-wsl-2

still issues.

Tried getting latest driver 515 using this guide.
https://tutorialforlinux.com/2020/06/01/nvidia-quadro-p1000-ubuntu-20-04-driver-installation/2/

Now accelerad finds the driver, but says no available GPUS. My P1000 is on the list of supported GPUs though
![image](https://user-images.githubusercontent.com/19936679/192715870-d8a72298-6a54-402d-93a3-7b9b2addce07.png)


next up trying this one  Linux->WSL
![image](https://user-images.githubusercontent.com/19936679/192715972-60e4e754-8ef4-443d-8b83-057ebd4a45a8.png)

```
wget https://developer.download.nvidia.com/compute/cuda/repos/wsl-ubuntu/x86_64/cuda-wsl-ubuntu.pin
sudo mv cuda-wsl-ubuntu.pin /etc/apt/preferences.d/cuda-repository-pin-600
wget https://developer.download.nvidia.com/compute/cuda/11.7.1/local_installers/cuda-repo-wsl-ubuntu-11-7-local_11.7.1-1_amd64.deb
sudo dpkg -i cuda-repo-wsl-ubuntu-11-7-local_11.7.1-1_amd64.deb
sudo cp /var/cuda-repo-wsl-ubuntu-11-7-local/cuda-*-keyring.gpg /usr/share/keyrings/
sudo apt-get update
sudo apt-get -y install cuda
```


tried the 
linux driver through the .run file and getting this error
https://www.nvidia.com/download/driverResults.aspx/193095/en-us/
![image](https://user-images.githubusercontent.com/19936679/192716451-ce9f51b7-6bbb-4ec2-9168-0a3539b827f3.png)
