﻿using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.IO;
using ImageSharp;

namespace Castles
{
    public static class Loader
    {
        public static readonly string defaultObj = "../../Resources/Models/";
        public static readonly string defaultTex = "../../Resources/Textures/";
        public static readonly string defaultSpec = "../../Resources/Specs/";


        public static Dictionary<string, Model> modelMap = new Dictionary<string, Model>();
        public static Dictionary<string, Texture> textureMap = new Dictionary<string, Texture>();

        public static Texture LoadTexture(string file)
        {
            if (!Path.HasExtension(file))
                file = file + ".png";
            file = file.Replace("!", defaultTex);

            string name = Path.GetFileNameWithoutExtension(file);
            Texture t;
            if (!textureMap.ContainsKey(name))
            {
                if (!File.Exists(file))
                    return null;

                textureMap.Add(name, t = new Texture(file));
            }
            else
                t = textureMap[name];



            Gl.BindTexture(t);
            Gl.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            Gl.TexParameteri(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, TextureParameter.LinearMipMapLinear);
            Gl.TexParameterf(TextureTarget.Texture2D, TextureParameterName.TextureLodBias, 0);

            if (Gl.IsExtensionSupported(Extension.GL_EXT_texture_filter_anisotropic))
                Gl.TexParameterf(TextureTarget.Texture2D, TextureParameterName.MaxAnisotropyExt,
                    Math.Min(4f,
                    Gl.GetFloat(GetPName.MaxTextureMaxAnisotropyExt)
                    )
                    );
            return t;
        }


        public static void Dispose()
        {
            foreach(var r in textureMap)
            {
                r.Value.Dispose();
            }
            foreach(var r in modelMap)
            {
                r.Value.Dispose();
            }
        }
        public static Model LoadModel(string obj, Texture tex, ShaderProgram program)
        {

            if (!Path.HasExtension(obj))
                obj = obj + ".obj";
            obj = obj.Replace("!", defaultObj);
            string name = Path.GetFileNameWithoutExtension(obj);
            if (modelMap.ContainsKey(name))
                return modelMap[name];

            VAO vao;
            VBO<Vector3> vertices;
            VBO<Vector3> normals;
            VBO<Vector2> texture;
            VBO<int> elements;

            List<Vector3> verticesList = new List<Vector3>();
            List<Vector3> normalsList = new List<Vector3>();
            List<Vector2> textureList = new List<Vector2>();
            List<int> elementList = new List<int>();


            IEnumerable<string> data = System.IO.File.ReadLines(obj);
            //read data
            foreach (string line in System.IO.File.ReadLines(obj))
            {
                if (line.StartsWith("v "))
                {
                    string[] s = line.Split(' ');
                    verticesList.Add(new Vector3(ToFloat(s[1]), ToFloat(s[2]), ToFloat(s[3])));
                }
                if (line.StartsWith("vn "))
                {
                    string[] s = line.Split(' ');
                    normalsList.Add(new Vector3(ToFloat(s[1]), ToFloat(s[2]), ToFloat(s[3])));
                }
                if (line.StartsWith("vt "))
                {
                    string[] s = line.Split(' ');
                    textureList.Add(new Vector2(ToFloat(s[1]), 1 - ToFloat(s[2])));
                }
            }

            //insert data
            Vector3[] normalsArray = new Vector3[verticesList.Count];
            Vector3[] verticesArray = verticesList.ToArray();
            Vector2[] textureArray = new Vector2[verticesList.Count];


            foreach (string line in File.ReadLines(obj))
            {
                if (line.StartsWith("f "))
                {
                    string[] strings = line.Split(' ');

                    if (strings.Length == 5)
                    {
                        foreach (string str in new string[] { strings[1], strings[2], strings[3] })
                        {
                            string[] strs = str.Split('/');
                            if (int.TryParse(strs[0], out int i1))
                            {
                                elementList.Add(i1 - 1);
                            }
                            if (int.TryParse(strs[1], out int i2))
                            {
                                textureArray[i1 - 1] = textureList[i2 - 1];
                            }
                            if (int.TryParse(strs[2], out int i3))
                            {
                                normalsArray[i1 - 1] = normalsList[i3 - 1];
                            }
                        }
                        foreach (string str in new string[] { strings[1], strings[3], strings[4] })
                        {
                            string[] strs = str.Split('/');
                            if (int.TryParse(strs[0], out int i1))
                            {
                                elementList.Add(i1 - 1);
                            }
                            if (int.TryParse(strs[1], out int i2))
                            {
                                textureArray[i1 - 1] = textureList[i2 - 1];
                            }
                            if (int.TryParse(strs[2], out int i3))
                            {
                                normalsArray[i1 - 1] = normalsList[i3 - 1];
                            }
                        }
                    }
                    else
                    {
                        foreach (string str in new string[] { strings[1], strings[2], strings[3] })
                        {
                            string[] strs = str.Split('/');
                            if (int.TryParse(strs[0], out int i1))
                            {
                                elementList.Add(i1 - 1);
                            }
                            if (int.TryParse(strs[1], out int i2))
                            {
                                textureArray[i1 - 1] = textureList[i2 - 1];
                            }
                            if (int.TryParse(strs[2], out int i3))
                            {
                                normalsArray[i1 - 1] = normalsList[i3 - 1];
                            }
                        }
                    }
                }
            }
            vertices = new VBO<Vector3>(verticesArray);
            normals = new VBO<Vector3>(normalsArray);
            texture = new VBO<Vector2>(textureArray, BufferTarget.TextureBuffer);
            elements = new VBO<int>(elementList.ToArray(), BufferTarget.ElementArrayBuffer);



            if (textureList.Count > 0 && texture != null)
                vao = new VAO(program, vertices, normals, texture, elements);
            else
                vao = new VAO(program, vertices, normals, elements);
            Model m = new Model(vao, tex);
            modelMap.Add(name, m);

            //specs
            CSV specs = new CSV(defaultSpec + Path.GetFileNameWithoutExtension(obj));
            if (specs.Any())
            {
                LoadSpec(s => m.Reflectivity = float.Parse(s.Replace('.', ',')), "reflectivity", specs);
                LoadSpec(s => m.ShineDamper = float.Parse(s.Replace('.', ',')), "shineDamper", specs);
            }
            return m;
        }

        private static void LoadSpec(Action<string> f, string name, CSV csv)
        {
            string s = csv?[name]?[1];
            if(s != null)
                f(s);
        }

        public static Model LoadModel(string obj, string tex, ShaderProgram program)
        { 
            if (!Path.HasExtension(tex))
                tex = tex + ".png";
            tex = tex.Replace("!", defaultTex);
            return LoadModel(obj, LoadTexture(tex), program);
        }
        public static Model LoadModel(string name, ShaderProgram program)
        {
            return LoadModel(name, name, program);
        }

        private static float ToFloat(string s)
        {
            return float.Parse(s.Replace('.', ','));
        }
    }
}
