
meshToRadiance = MSHToRAD(mesh, radFileName, workingDir, HDRTexture, RADMaterial)
    objFile = meshToRadiance.meshToObj()
    materialFile, radianceFile = meshToRadiance.objToRAD(objFile)


class hb_MSHToRAD(object):
    
    def __init__(self, mesh, fileName = None, workingDir = None, bitmap = None, radMaterial = None):
        
        if fileName == None:
            fileName = "unnamed"
        
        self.name = fileName
        
        if workingDir == None:
            workingDir = sc.sticky["Honeybee_DefaultFolder"]
        
        workingDir = os.path.join(workingDir, fileName, "MSH2RADFiles")
        if not os.path.isdir(workingDir): os.mkdir(workingDir)
        self.workingDir = workingDir
        
        self.mesh = mesh
        
        self.RadianceFolder = sc.sticky["honeybee_folders"]["RADPath"]
         
        self.pattern = bitmap
        if self.pattern != None:
            # create material name based on bitmap
            bitmapFileName = os.path.basename(self.pattern)
            self.matName = ".".join(bitmapFileName.split(".")[:-1])
            #copy the image into same folder
            try:
                shutil.copyfile(self.pattern, os.path.join(self.workingDir, bitmapFileName))
            except:
                pass
        else:
            self.matName = "radMaterial"
            if radMaterial != None:
                radMaterial = RADMaterialAux.getRadianceObjectsFromString(radMaterial)[0]
                try:
                    self.matName = radMaterial.strip().split()[2]
                    assert self.matName != ""
                except:
                    raise Exception("Failed to import %s. Double check the material definition."%radMaterial)

        self.RADMaterial = " ".join(radMaterial.split())
        
    def meshToObj(self):
        objFilePath = os.path.join(self.workingDir, self.name + ".obj")
        
        with open(objFilePath, "w") as outfile:
            
            # objTxt = "# OBJ file written by TurtlePyMesh\n\n"
            outfile.write("# OBJ file written by TurtlePyMesh\n\n")
            
            # add material file name
            mtlFile = self.name + ".mtl"
            #objTxt += "mtllib " + mtlFile + "\n"
            outfile.write("mtllib " + mtlFile + "\n")
            
            for count, Tmesh in enumerate(self.mesh):
                # add object name - for this version I keep it all as a single object
                #objTxt += "o object_" + str(count + 1) + "\n"
                outfile.write("o object_" + str(count + 1) + "\n")
                
                # add material name - for now brick as test
                #objTxt += "usemtl " + matName + "\n"
                outfile.write("usemtl " + self.matName + "\n")
                
                if Tmesh.Normals.Count == 0:
                    Tmesh.Normals.ComputeNormals()
                    
                # add vertices
                for v in Tmesh.Vertices:
                    XYZ = v.X, v.Y, v.Z
                    XYZ = map(str, XYZ)
                    vString = " ".join(XYZ)
                    #objTxt += "v "  + vString + "\n"
                    outfile.write("v "  + vString + "\n")
                # add texture vertices
                for vt in Tmesh.TextureCoordinates:
                    XY = vt.X, vt.Y
                    XY = map(str, XY)
                    vtString = " ".join(XY)
                    #objTxt += "vt "  + vtString + "\n"
                    outfile.write("vt "  + vtString + "\n")
                # add normals
                for vn in Tmesh.Normals:
                    XYZ = vn.X, vn.Y, vn.Z
                    XYZ = map(str, XYZ)
                    vnString = " ".join(XYZ)
                    # objTxt += "vn "  + vnString + "\n"
                    outfile.write("vn "  + vnString + "\n")
                # add faces
                # vertices number is global so the number should be added together
                fCounter = 0
                
                if count > 0:
                    for meshCount in range(count):
                        fCounter += self.mesh[meshCount].Vertices.Count
                
                # print fCounter
                if self.pattern != None:
                    for face in Tmesh.Faces:
                        # objTxt += "f " + "/".join(3*[`face.A  + fCounter + 1`]) + " " + "/".join(3*[`face.B + fCounter + 1`]) + " " + "/".join(3*[`face.C + fCounter + 1`])
                        outfile.write("f " + "/".join(3*[`face.A  + fCounter + 1`]) + " " + "/".join(3*[`face.B + fCounter + 1`]) + " " + "/".join(3*[`face.C + fCounter + 1`]))
                        if face.IsQuad:
                            #objTxt += " " + "/".join(3*[`face.D + fCounter + 1`])
                            outfile.write(" " + "/".join(3*[`face.D + fCounter + 1`]))
                            
                        #objTxt += "\n"
                        outfile.write("\n")
                else:
                    for face in Tmesh.Faces:
                        outfile.write("f " + "//".join(2 * [`face.A  + fCounter + 1`]) + \
                                      " " + "//".join(2 * [`face.B + fCounter + 1`]) + \
                                      " " + "//".join(2 * [`face.C + fCounter + 1`]))
                        
                        if face.IsQuad:
                            outfile.write(" " + "//".join( 2 * [`face.D + fCounter + 1`]))
                            
                        #objTxt += "\n"
                        outfile.write("\n")
                        
        # This method happened to be so slow!
        #    with open(objFile, "w") as outfile:
        #        outfile.writelines(objTxt)
        
        return objFilePath
    
    def getPICImageSize(self):
        with open(self.pattern, "rb") as inf:
            for count, line in enumerate(inf):
                #print line
                if line.strip().startswith("-Y") and line.find("-X"):
                    Y, YSize, X, XSize = line.split(" ")
                    return XSize, YSize
    
    def objToRAD(self, objFile):
        # prepare file names
        radFile = objFile.replace(".obj", ".rad")
        mshFile = objFile.replace(".obj", ".msh")
        batFile = objFile.replace(".obj", ".bat")        
        
        path, fileName = os.path.split(radFile)
        matFile = os.path.join(path, "material_" + fileName)
        
        try:
            materialType = self.RADMaterial.split()[1]
            materialTale = " ".join(self.RADMaterial.split()[3:])
        except Exception, e:
            # to be added here: if material is not full string then get it from the library
            errmsg = "Failed to parse material:\n%s" % e
            print errmsg
            raise ValueError(errmsg)
        # create material file
        if self.pattern != None:
            
            # find aspect ratio
            try:
                X, Y= self.getPICImageSize()
                ar = str(int(X)/int(Y))
            except Exception, e:
                ar = str(1)
            
            # mesh has a pattern
            patternName = ".".join(os.path.basename(self.pattern).split(".")[:-1])
            
            materialStr = "void colorpict " + patternName + "_pattern\n" + \
                  "7 red green blue " + self.pattern + " . (" + ar + "*(Lu-floor(Lu))) (Lv-floor(Lv)) \n" + \
                  "0\n" + \
                  "1 1\n" + \
                  patternName + "_pattern " + materialType + " " + patternName + "\n" + \
                  materialTale
        else:
            materialStr = "void "  + materialType + " " + self.matName + " " +  \
                  materialTale  
                  
        # write material to file
        with open(matFile, "w") as outfile:
            outfile.write(materialStr)
        
        # create rad file
        
        if self.pattern != None:
            cmd = self.RadianceFolder + "\\obj2mesh -a " + matFile + " " + objFile + " > " +  mshFile
            
            with open(batFile, "w") as outfile:
                outfile.write(cmd)
                #outfile.write("\npause")
                
            os.system(batFile)
            
            radStr = "void mesh painting\n" + \
                     "1 " + mshFile + "\n" + \
                     "0\n" + \
                     "0\n"
            
            with open(radFile, "w") as outfile:
                outfile.write(radStr)
        else:
            # use object to rad
            #create a fake mtl file - material will be overwritten by radiance material
            mtlFile = objFile.replace(".obj", ".mtl")
            
            mtlStr = "# Honeybee\n" + \
                     "newmtl " + self.matName + "\n" + \
                     "Ka 0.0000 0.0000 0.0000\n" + \
                     "Kd 1.0000 1.0000 1.0000\n" + \
                     "Ks 1.0000 1.0000 1.0000\n" + \
                     "Tf 0.0000 0.0000 0.0000\n" + \
                     "d 1.0000\n" + \
                     "Ns 0\n"
            
            with open(mtlFile, "w") as mtlf:
                mtlf.write(mtlStr)
            
            # create a map file
            #mapFile = objFile.replace(".obj", ".map")
            #with open(mapFile, "w") as mapf:
            #    mapf.write(self.matName + " (Object \"" + self.matName + "\");")
            #cmd = "c:\\radiance\\bin\\obj2rad -m " + mapFile + " " + objFile + " > " +  radFile
            
            cmd = self.RadianceFolder + "\\obj2rad -f " + objFile + " > " +  radFile
            
            with open(batFile, "w") as outfile:
                outfile.write(cmd)
                #outfile.write("\npause")
                
            os.system(batFile)
            
        time.sleep(.2)
    
        return matFile, radFile
