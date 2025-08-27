using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Drawing;
using System.Drawing.Imaging;
using MyPuzzleGame.Core;

namespace MyPuzzleGame.Rendering
{
    public class ShaderProgram : IDisposable
    {
        private int _program;
        private bool _disposed = false;

        public int Handle => _program;

        public ShaderProgram(string vertexSource, string fragmentSource)
        {
            int vertexShader = CreateShader(ShaderType.VertexShader, vertexSource);
            int fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentSource);

            _program = GL.CreateProgram();
            GL.AttachShader(_program, vertexShader);
            GL.AttachShader(_program, fragmentShader);
            GL.LinkProgram(_program);

            GL.GetProgram(_program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(_program);
                throw new Exception($"Error linking shader program: {infoLog}");
            }

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        private static int CreateShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Error compiling {type} shader: {infoLog}");
            }

            return shader;
        }

        public void Use()
        {
            GL.UseProgram(_program);
        }

        public void SetMatrix4(string name, Matrix4 matrix)
        {
            int location = GL.GetUniformLocation(_program, name);
            GL.UniformMatrix4(location, false, ref matrix);
        }

        public void SetVector3(string name, Vector3 vector)
        {
            int location = GL.GetUniformLocation(_program, name);
            GL.Uniform3(location, vector);
        }

        public void SetVector2(string name, Vector2 vector)
        {
            int location = GL.GetUniformLocation(_program, name);
            GL.Uniform2(location, vector);
        }

        public void SetFloat(string name, float value)
        {
            int location = GL.GetUniformLocation(_program, name);
            GL.Uniform1(location, value);
        }

        public void SetInt(string name, int value)
        {
            int location = GL.GetUniformLocation(_program, name);
            GL.Uniform1(location, value);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteProgram(_program);
                _disposed = true;
            }
        }
    }

    public class GPURenderer : IDisposable
    {
        private ShaderProgram? _blockShader;
        private ShaderProgram? _uiShader; // Renamed from _textShader for clarity
        private int _quadVAO, _quadVBO; // Renamed from _blockVAO, _blockVBO
        private Matrix4 _projectionMatrix;
        private bool _disposed = false;

        private readonly float[] _quadVertices = new float[]
        {
            // Position (x, y)    // UV coords
            0.0f, 0.0f,           0.0f, 0.0f,
            1.0f, 0.0f,           1.0f, 0.0f,
            1.0f, 1.0f,           1.0f, 1.0f,
            0.0f, 1.0f,           0.0f, 1.0f
        };

        private readonly uint[] _quadIndices = new uint[]
        {
            0, 1, 2,
            2, 3, 0
        };

        public void Initialize(int windowWidth, int windowHeight)
        {
            try
            {
                InitializeShaders();
                InitializeBuffers();
                UpdateProjection(windowWidth, windowHeight);
                
                Console.WriteLine("GPU Renderer initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing GPU renderer: {ex.Message}");
                throw;
            }
        }

        public void UpdateProjection(int windowWidth, int windowHeight)
        {
            _projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, windowWidth, windowHeight, 0, -1.0f, 1.0f);
        }

        private void InitializeShaders()
        {
            string blockVertexShader = @"
#version 330 core
layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoord;

uniform mat4 uProjection;
uniform mat4 uModel;

out vec2 TexCoord;

void main()
{
    gl_Position = uProjection * uModel * vec4(aPosition, 0.0, 1.0);
    TexCoord = aTexCoord;
}";

            string blockFragmentShader = @"
#version 330 core
in vec2 TexCoord;

uniform vec3 uColor;
uniform float uBrightness;
uniform sampler2D uTexture;
uniform bool uUseTexture;

out vec4 FragColor;

void main()
{
    vec4 texColor = texture(uTexture, TexCoord);
    vec3 finalColor = uColor * uBrightness;
    if (uUseTexture) {
        FragColor = texColor * vec4(finalColor, 1.0);
    } else {
        FragColor = vec4(finalColor, 1.0);
    }
}";

            string uiFragmentShader = @"
#version 330 core
uniform vec3 uColor;
out vec4 FragColor;
void main()
{
    FragColor = vec4(uColor, 1.0);
}";

            _blockShader = new ShaderProgram(blockVertexShader, blockFragmentShader);
            _uiShader = new ShaderProgram(blockVertexShader, uiFragmentShader);
        }

        private void InitializeBuffers()
        {
            _quadVAO = GL.GenVertexArray();
            _quadVBO = GL.GenBuffer();
            int quadEBO = GL.GenBuffer();

            GL.BindVertexArray(_quadVAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _quadVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, _quadVertices.Length * sizeof(float), _quadVertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, quadEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _quadIndices.Length * sizeof(uint), _quadIndices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

        public int LoadTexture(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Texture file not found", filePath);
            }

            int textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            using (var bmp = new Bitmap(filePath))
            {
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                bmp.UnlockBits(data);
            }

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            return textureId;
        }

        public void RenderBlock(int x, int y, int size, Vector3 color, float brightness = 1.0f)
        {
            if (_blockShader == null) return;
            _blockShader.Use();
            Matrix4 model = Matrix4.CreateScale(size, size, 1.0f) * Matrix4.CreateTranslation(x, y, 0.0f);
            _blockShader.SetMatrix4("uProjection", _projectionMatrix);
            _blockShader.SetMatrix4("uModel", model);
            _blockShader.SetVector3("uColor", color);
            _blockShader.SetFloat("uBrightness", brightness);
            _blockShader.SetInt("uUseTexture", 0); // false

            GL.BindVertexArray(_quadVAO);
            GL.DrawElements(PrimitiveType.Triangles, _quadIndices.Length, DrawElementsType.UnsignedInt, 0);
        }

        public void RenderQuad(int x, int y, int width, int height, Vector3 color)
        {
            if (_uiShader == null) return;
            _uiShader.Use();
            Matrix4 model = Matrix4.CreateScale(width, height, 1.0f) * Matrix4.CreateTranslation(x, y, 0.0f);
            _uiShader.SetMatrix4("uProjection", _projectionMatrix);
            _uiShader.SetMatrix4("uModel", model);
            _uiShader.SetVector3("uColor", color);

            GL.BindVertexArray(_quadVAO);
            GL.DrawElements(PrimitiveType.Triangles, _quadIndices.Length, DrawElementsType.UnsignedInt, 0);
        }

        public void RenderTexturedQuad(int x, int y, int width, int height, int textureId)
        {
            if (_blockShader == null) return;
            _blockShader.Use();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            Matrix4 model = Matrix4.CreateScale(width, height, 1.0f) * Matrix4.CreateTranslation(x, y, 0.0f);
            _blockShader.SetMatrix4("uProjection", _projectionMatrix);
            _blockShader.SetMatrix4("uModel", model);
            _blockShader.SetVector3("uColor", Vector3.One); // Use white as base color for texture
            _blockShader.SetFloat("uBrightness", 1.0f);
            _blockShader.SetInt("uTexture", 0);
            _blockShader.SetInt("uUseTexture", 1); // true

            GL.BindVertexArray(_quadVAO);
            GL.DrawElements(PrimitiveType.Triangles, _quadIndices.Length, DrawElementsType.UnsignedInt, 0);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _blockShader?.Dispose();
                _uiShader?.Dispose();
                GL.DeleteVertexArray(_quadVAO);
                GL.DeleteBuffer(_quadVBO);
                _disposed = true;
            }
        }
    }

    public static class GPUBlockColors
    {
        public static readonly Dictionary<Core.BlockType, (Vector3 Main, Vector3 Light, Vector3 Dark)> Colors = new()
        {
            [Core.BlockType.Red] = (new Vector3(255f/255f, 59f/255f, 48f/255f), new Vector3(255f/255f, 59f/255f, 48f/255f), new Vector3(255f/255f, 59f/255f, 48f/255f)),
            [Core.BlockType.Green] = (new Vector3(52f/255f, 199f/255f, 89f/255f), new Vector3(52f/255f, 199f/255f, 89f/255f), new Vector3(52f/255f, 199f/255f, 89f/255f)),
            [Core.BlockType.Blue] = (new Vector3(0f/255f, 122f/255f, 255f/255f), new Vector3(0f/255f, 122f/255f, 255f/255f), new Vector3(0f/255f, 122f/255f, 255f/255f)),
            [Core.BlockType.Yellow] = (new Vector3(255f/255f, 204f/255f, 0f/255f), new Vector3(255f/255f, 204f/255f, 0f/255f), new Vector3(255f/255f, 204f/255f, 0f/255f)),
            [Core.BlockType.Purple] = (new Vector3(175f/255f, 82f/255f, 222f/255f), new Vector3(175f/255f, 82f/255f, 222f/255f), new Vector3(175f/255f, 82f/255f, 222f/255f)),
            [Core.BlockType.Orange] = (new Vector3(255f/255f, 149f/255f, 0f/255f), new Vector3(255f/255f, 149f/255f, 0f/255f), new Vector3(255f/255f, 149f/255f, 0f/255f))
        };
    }
}