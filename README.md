# GrasshopperRadianceLinuxConnector
an educational tool to work directly with radiance through grasshopper


## Preparations

### Setup Windows Subsystem Linux (WSL) and radiance

* Start enabling WSL and installing Radiance and XLaunch as per [this link](https://www.mattiabressanelli.com/engineering/linux-radiance-on-windows-with-wsl-and-x11/).
  Don't make your username too long and dont make your password too complex. Do not make the PW the same as your windows user.

* Install CShell using 
*         $ sudo apt-get install csh
* Install libqt5 (a GUI program for some of the radiance GUIs) using:
*         $ sudo apt-get install libqt5gui5
* Setup symbolic links to access your windows simulation folder in linux. (ie, making a shortcut to ~/simulation in linux to target your C:\users\<username>\Simulation in windows)
*         $ ln -s /mnt/c/Users/<username>/simulation ~/simulation
* Now you can always find your simulations by typinc
*         cd ~/simulation
* I assume you installed Xlaunch as per the first tutorial. Now start it as described in the tutorial.
* Make sure your linux is pointing towards the XLaunch display driver by adding it to the .bashrc file:
*         sudo nano ~/.bashrc
* This will open the bashrc file. Go to the end of the file and add these two lines. (I cant remember how to paste, so you'll type them manually):
*         export LIBGL_ALWAYS_INDIRECT=1
          export DISPLAY=$(ip route list default | awk ‘{print $3}’):0
