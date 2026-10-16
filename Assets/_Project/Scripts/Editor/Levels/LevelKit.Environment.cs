using BugReport.EditorTools.Setup;
using UnityEngine;
using static BugReport.EditorTools.Setup.TestRoomBuilder;

namespace BugReport.EditorTools.Levels
{
    internal sealed partial class LevelKit
    {
        private readonly ArchitectureDresser dresser = new ArchitectureDresser();
        private System.Collections.Generic.List<GameObject> floorBlocks => dresser.floorBlocks;
        private System.Collections.Generic.List<(float from, float to)> floorSegments => dresser.floorSegments;

        /// <summary>A solid wall block (collider + stand-in visual). <paramref name="dress"/>: also receives torches and banners.</summary>
        private GameObject WallBlock(string name, Vector3 position, Vector3 size, bool dress = true)
        {
            var go = Block(name, Env, position, size, Mats.Wall, World);
            dresser.wallBlocks.Add((go, dress));
            return go;
        }

        private void DressArchitecture()
        {
            dresser.HalfZ = HalfZ;
            dresser.Chapter = Chapter;
            dresser.Dress(Env);
        }
    }
}
