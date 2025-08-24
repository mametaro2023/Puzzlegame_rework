using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Text;
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
        private ShaderProgram? _textShader;
        private int _blockVAO, _blockVBO;
        private int _textVAO, _textVBO;
        private Matrix4 _projectionMatrix;
        private bool _disposed = false;

        // Block vertex data (position + color)
        private readonly float[] _blockVertices = new float[]
        {
            // Position (x, y)    // UV coords
            0.0f, 0.0f,           0.0f, 0.0f,
            1.0f, 0.0f,           1.0f, 0.0f,
            1.0f, 1.0f,           1.0f, 1.0f,
            0.0f, 1.0f,           0.0f, 1.0f
        };

        private readonly uint[] _blockIndices = new uint[]
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
            // Use left-top origin coordinate system to match CPU rendering
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

out vec4 FragColor;

void main()
{
    // Create gradient effect based on UV coordinates
    float gradient = 1.0 - (TexCoord.x * 0.2 + TexCoord.y * 0.2);
    vec3 finalColor = uColor * (uBrightness * gradient);
    FragColor = vec4(finalColor, 1.0);
}";

            string textVertexShader = @"
#version 330 core
layout (location = 0) in vec2 aPosition;

uniform mat4 uProjection;
uniform vec2 uPosition;
uniform vec2 uSize;

void main()
{
    vec2 pos = aPosition * uSize + uPosition;
    gl_Position = uProjection * vec4(pos, 0.0, 1.0);
}";

            string textFragmentShader = @"
#version 330 core
uniform vec3 uColor;

out vec4 FragColor;

void main()
{
    FragColor = vec4(uColor, 1.0);
}";

            _blockShader = new ShaderProgram(blockVertexShader, blockFragmentShader);
            _textShader = new ShaderProgram(textVertexShader, textFragmentShader);
        }

        private void InitializeBuffers()
        {
            // Block rendering setup
            _blockVAO = GL.GenVertexArray();
            _blockVBO = GL.GenBuffer();
            int blockEBO = GL.GenBuffer();

            GL.BindVertexArray(_blockVAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _blockVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, _blockVertices.Length * sizeof(float), _blockVertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, blockEBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _blockIndices.Length * sizeof(uint), _blockIndices, BufferUsageHint.StaticDraw);

            // Position attribute
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // UV attribute
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // Text rendering setup (simple quad)
            _textVAO = GL.GenVertexArray();
            _textVBO = GL.GenBuffer();

            float[] textVertices = new float[]
            {
                0.0f, 0.0f,
                1.0f, 0.0f,
                1.0f, 1.0f,
                0.0f, 1.0f
            };

            GL.BindVertexArray(_textVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _textVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, textVertices.Length * sizeof(float), textVertices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindVertexArray(0);
        }

        public void RenderBlock(int x, int y, int size, Vector3 color, float brightness = 1.0f)
        {
            if (_blockShader == null) return;

            _blockShader.Use();

            // Create model matrix for position and scale
            Matrix4 model = Matrix4.CreateScale(size, size, 1.0f) * Matrix4.CreateTranslation(x, y, 0.0f);
            
            _blockShader.SetMatrix4("uProjection", _projectionMatrix);
            _blockShader.SetMatrix4("uModel", model);
            _blockShader.SetVector3("uColor", color);
            _blockShader.SetFloat("uBrightness", brightness);

            GL.BindVertexArray(_blockVAO);
            GL.DrawElements(PrimitiveType.Triangles, _blockIndices.Length, DrawElementsType.UnsignedInt, 0);
        }

        public void RenderQuad(int x, int y, int width, int height, Vector3 color)
        {
            if (_textShader == null) return;

            _textShader.Use();
            _textShader.SetMatrix4("uProjection", _projectionMatrix);
            _textShader.SetVector3("uColor", color);
            _textShader.SetVector2("uPosition", new Vector2(x, y));
            _textShader.SetVector2("uSize", new Vector2(width, height));

            GL.BindVertexArray(_textVAO);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
        }


        public void Dispose()
        {
            if (!_disposed)
            {
                _blockShader?.Dispose();
                _textShader?.Dispose();

                GL.DeleteVertexArray(_blockVAO);
                GL.DeleteBuffer(_blockVBO);
                GL.DeleteVertexArray(_textVAO);
                GL.DeleteBuffer(_textVBO);

                _disposed = true;
            }
        }
    }

    public static class GPUBlockColors
    {
        public static readonly Dictionary<Core.BlockType, (Vector3 Main, Vector3 Light, Vector3 Dark)> Colors = new()
        {
            [Core.BlockType.Red] = (
                new Vector3(220f/255f, 20f/255f, 60f/255f),
                new Vector3(255f/255f, 99f/255f, 132f/255f),
                new Vector3(139f/255f, 0f/255f, 0f/255f)
            ),
            [Core.BlockType.Green] = (
                new Vector3(34f/255f, 139f/255f, 34f/255f),
                new Vector3(144f/255f, 238f/255f, 144f/255f),
                new Vector3(0f/255f, 100f/255f, 0f/255f)
            ),
            [Core.BlockType.Blue] = (
                new Vector3(30f/255f, 144f/255f, 255f/255f),
                new Vector3(135f/255f, 206f/255f, 250f/255f),
                new Vector3(0f/255f, 0f/255f, 139f/255f)
            ),
            [Core.BlockType.Yellow] = (
                new Vector3(255f/255f, 215f/255f, 0f/255f),
                new Vector3(255f/255f, 255f/255f, 224f/255f),
                new Vector3(184f/255f, 134f/255f, 11f/255f)
            ),
            [Core.BlockType.Purple] = (
                new Vector3(147f/255f, 112f/255f, 219f/255f),
                new Vector3(221f/255f, 160f/255f, 221f/255f),
                new Vector3(75f/255f, 0f/255f, 130f/255f)
            ),
            [Core.BlockType.Orange] = (
                new Vector3(255f/255f, 140f/255f, 0f/255f),
                new Vector3(255f/255f, 218f/255f, 185f/255f),
                new Vector3(205f/255f, 92f/255f, 92f/255f)
            )
        };
    }
}