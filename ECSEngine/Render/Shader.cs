﻿using OpenGL;
using System.IO;

namespace ECSEngine.Render
{
    /// <summary>
    /// An individual shader that is contained within a <see cref="Components.ShaderComponent"/>.
    /// </summary>
    public struct Shader
    {
        /// <summary>
        /// The shader source's file name.
        /// </summary>
        public string fileName { get; set; }

        /// <summary>
        /// OpenGL's reference to the shader.
        /// </summary>
        public uint glShader { get; set; }

        /// <summary>
        /// The shader's type.
        /// </summary>
        public ShaderType shaderType { get; set; }

        private string[] shaderSource_;

        /// <summary>
        /// The shader's source.
        /// </summary>
        public string[] shaderSource
        {
            get => shaderSource_;
            set
            {
                shaderSource_ = value;
                Compile();
            }
        }

        /// <summary>
        /// Construct an instance of <see cref="Shader"/>, compile the shader and check for any errors.
        /// </summary>
        /// <param name="path">The shader source's file name.</param>
        /// <param name="shaderType">The shader's type.</param>
        public Shader(string path, ShaderType shaderType)
        {
            this.shaderType = shaderType;
            fileName = path;
            glShader = Gl.CreateShader(shaderType);
            shaderSource_ = new string[0];

            ReadSourceFromFile();
            Compile();
        }

        public void ReadSourceFromFile()
        {
            shaderSource_ = new string[1];
            using (StreamReader streamReader = new StreamReader(fileName))
                shaderSource_[0] = streamReader.ReadToEnd();
        }

        /// <summary>
        /// Read the shader source, and then compile it.
        /// </summary>
        public void Compile()
        {
            Gl.ShaderSource(glShader, shaderSource);
            Gl.CompileShader(glShader);

            CheckForErrors();
        }

        /// <summary>
        /// Check to ensure that the shader compilation was successful; display any errors otherwise.
        /// </summary>
        private void CheckForErrors()
        {
            var glErrors = Gl.GetError();
            if (glErrors != ErrorCode.NoError)
            {
                int maxLength = 1024;
                var glErrorStr = new System.Text.StringBuilder(maxLength);
                Gl.GetShaderInfoLog(glShader, maxLength, out int length, glErrorStr);
                Debug.Log($"Problem compiling shader {fileName}: ({length}) {glErrors} - {glErrorStr}", Debug.DebugSeverity.High);
            }
            else
            {
                Debug.Log($"Compiled shader {fileName} successfully");
            }
        }
    }
}
