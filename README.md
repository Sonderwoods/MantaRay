## Project MantaRay (GrasshopperRadianceLinuxConnector)
An educational tool to work directly with [Radiance](https://www.radiance-online.org/) through [Grasshopper](https://www.grasshopper3d.com/).
This allows you to utilize all the linux native Radiance commands including the GUI tools such as rvu, bsdfviewer etc.

In pure Linux fashion the Grasshopper components will pipe the results to the next component.

Async SSH Components in place (based on the [speckle async](https://github.com/specklesystems/GrasshopperAsyncComponent))
![radianceasync](https://user-images.githubusercontent.com/19936679/166122160-9a706a61-eaa1-48cb-a5a4-6f95681a83a0.gif)



## Components:
#### SSH Connect
![radianceConnect](https://user-images.githubusercontent.com/19936679/168886070-7af082d4-ba57-417e-a5ff-9d93269a21de.gif)
- Connects to a SSH. Can embed password in GH script (internalize) or will prompt on execution.
- Will automatically run _bash sudo service ssh start_ if it's not already running (will ask before so)
- Will keep password even if you say recompute all. To set up a new selection set it to false and then true.

#### RadViewer
![radviewer2](https://user-images.githubusercontent.com/19936679/173672403-4fa20162-f701-47da-ac44-49b408e8f994.gif)



![radianceRadViewer](https://user-images.githubusercontent.com/19936679/168884796-fea8f5e5-f919-4222-81e4-dd676ce3794f.gif)
- Super fast parsing of multiple rad files
- Preview of random colors (Todo: Match color of radiance material)
- Todo: Legend for the colors
- Todo (Optional): Clickable objects to get modifiers

#### Mesh2obj (and obj2rad)
![image](https://user-images.githubusercontent.com/19936679/158892631-188c4ab0-b364-4b0c-820a-eff9101058e2.png)
- Runs in parallel and is fast
- Orients normals (vertice order in radiance) to match rhino mesh normals


#### Setup Globals (and Apply Globals)
![image](https://user-images.githubusercontent.com/19936679/168888836-58a91cee-17a5-409a-92de-a700d684b3af.png)
- A place to set your variables and reuse them, ie project folder etc.
- Can also be used for local replacements in the ssh command
- Only one Setup Globals component can be placed at the canvas at the same time (otherwise see screenshot)

#### Execute Async
![radianceExecuteAsync](https://user-images.githubusercontent.com/19936679/168891590-600b9434-834d-4f06-8166-10fa62cdab09.gif)
- The component that does the magic.
- Reuses outputs and saves them on your canvas. This means that when reopening grasshopper, you don't have to run all commands again if the temporary files are still there.
- Runs asyncronously, which means it does not block the grasshopper UI while it's running.
- Two components next to each other depending on the same inputs will both run in parallel and first trigger the downstream components when both are finished (use the RAN output as it becomes true when finished).
- To avoid this behavior, you can graft several inputs into the same component instead.
- Access to ImageMagick and the meta functions in linux only Radiance
![image](https://user-images.githubusercontent.com/19936679/159573035-72523b98-e2ad-40d1-ae82-ecc9f5068288.png)

#### CreateView
![image](https://user-images.githubusercontent.com/19936679/169893709-d9d29494-6bf3-48ca-b690-c583e038e1c1.png)
- Input a perspective named view from Rhino or use the active viewport
- Outputs the data for a viewfile. Can be echoed into a .vf file.
- Previews your camera in Rhino viewport in red. Green if the component is selected.

#### ManPages
![ManPage](https://user-images.githubusercontent.com/19936679/179314601-eb0dc6d2-f30a-4bd6-bdc4-8a021f8c2f39.gif)
- ManPages show the help files relevant to your promt
- They are live loaded from https://floyd.lbl.gov/radiance/ 


## Todo:
Check out the issues on my todo list [here](https://github.com/Sonderwoods/GrasshopperRadianceLinuxConnector/issues).



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
      export DISPLAY=$(ip route list default | awk '{print $3}'):0
      
* Get the meta files to work such as meta2tga and meta2tif:
  Download the auxiliary files from [https://github.com/LBNL-ETA/Radiance/releases](https://github.com/LBNL-ETA/Radiance/releases/download/05eb231c/Radiance_Auxiliary_05eb231c.zip)
  Place them in simulation folder on windows
  CHMOD the lib folder like below.
  This may allow everyone to change your lib folder!
  You should look into what is the default chmod setting for the lib folder.. I did overwrite mine, so I'm unsure.
  This will give us write access to our lib folder.
  
      $ sudo chmod 777 /usr/local/lib
  Then copy the meta files to the lib folder using:
      
      $ cp -R ~/simulation/meta /usr/local/lib/meta
      
* Install ImageMagick. This will grant you access to mogrify and convert. See more in the [Radiance Tutorial](http://www.jaloxa.eu/resources/radiance/documentation/docs/radiance_tutorial.pdf)


      $ sudo apt update
      $ sudo apt install imagemagick
      

### Setup SSH in linux and connect to it from Windows

* Follow the tutorial on this [link](https://www.illuminiastudios.com/dev-diaries/ssh-on-windows-subsystem-for-linux/). However I didnt manage to get the disable password part to work. Confirm that it's working by starting PowerShell and type:

      ssh <mylinuxusername>@127.0.0.1

If it prompts you for your password you should be good!
