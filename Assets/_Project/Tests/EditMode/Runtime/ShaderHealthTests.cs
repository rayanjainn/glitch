using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BugReport.Tests.EditMode
{
    /// <summary>The custom shaders must exist and compile; a broken shader renders magenta in builds.</summary>
    public class ShaderHealthTests
    {
        [TestCase("BugReport/Hologram")]
        [TestCase("BugReport/GradientSky")]
        public void CustomShader_Exists_AndHasNoErrors(string name)
        {
            var shader = Shader.Find(name);
            Assert.IsNotNull(shader, name + " not found; check Assets/_Project/Shaders");
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), name + " has compile errors; open it in the Inspector");
        }
    }
}
