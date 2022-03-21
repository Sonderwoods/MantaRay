## GrasshopperRadianceLinuxConnector
An educational tool to work directly with [Radiance](https://www.radiance-online.org/) through [Grasshopper](https://www.grasshopper3d.com/).
This allows you to utilize all the linux native Radiance commands including the GUI tools such as rvu, bsdfviewer etc.

In pure Linux fashion the Grasshopper components will pipe the results to the next component.

## Status:

SSH Connection in Grasshopper is now established
![image](https://user-images.githubusercontent.com/19936679/158470596-a3eab3e0-bc0a-447b-bd46-eba8a3877785.png)

![image](https://user-images.githubusercontent.com/19936679/158892497-523d0546-15f3-4ed2-8a1c-1a44e2777272.png)

Parallel Meshing and mesh2obj creation (in gh)
![image](https://user-images.githubusercontent.com/19936679/158892631-188c4ab0-b364-4b0c-820a-eff9101058e2.png)



## Preparations

### Setup Windows Subsystem Linux (WSL) and radiance

* Start enabling WSL and installing Radiance and XLaunch as per [this link](https://www.mattiabressanelli.com/engineering/linux-radiance-on-windows-with-wsl-and-x11/).
  Don't make your username too long and dont make your password too complex. Do not make the PW the same as your windows user.

* Install CShell using 

      $ sudo apt-get install csh
* Install libqt5 (a GUI program for some of the radiance GUIs) using:

      $ sudo apt-get install libqt5gui5
* Setup symbolic links to access your windows simulation folder in linux. (ie, making a shortcut to ~/simulation in linux to target your C:\users\<username>\Simulation in windows)

      $ ln -s /mnt/c/Users/<username>/simulation ~/simulation
* Now you can always find your simulations by typinc

      $ cd ~/simulation
* I assume you installed Xlaunch as per the first tutorial. Now start it as described in the tutorial.
* Make sure your linux is pointing towards the XLaunch display driver by adding it to the .bashrc file:

      $ sudo nano ~/.bashrc
* This will open the bashrc file. Go to the end of the file and add these two lines. (I cant remember how to paste, so you'll type them manually):

      export LIBGL_ALWAYS_INDIRECT=1
      export DISPLAY=$(ip route list default | awk ‘{print $3}’):0
      
* Get the meta files to work such as meta2tga and meta2tif:
  Download the auxiliary files from [https://github.com/LBNL-ETA/Radiance/releases](https://github.com/LBNL-ETA/Radiance/releases/download/05eb231c/Radiance_Auxiliary_05eb231c.zip)
  Place them in simulation folder on windows
  CHMOD the lib folder like below.
  This may allow everyone to change your lib folder!
  You should look into what is the default chmod setting for the lib folder.. I did overwrite mine, so I'm unsure.
  This will give us write access to our lib folder.
  
      sudo chmod 777 /usr/local/lib
  Then copy the meta files to the lib folder using:
      
      cp -R ~/simulation/meta /usr/local/lib/meta

### Setup SSH in linux and connect to it from Windows

* Follow the tutorial on this [link](https://www.illuminiastudios.com/dev-diaries/ssh-on-windows-subsystem-for-linux/). However I didnt manage to get the disable password part to work. Confirm that it's working by starting PowerShell and type:

      ssh <mylinuxusername>@127.0.0.1

If it prompts you for your password you should be good!
