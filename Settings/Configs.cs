using UnityEngine;

namespace ContentBoys
{
    public class Configs
    {
        public static bool infinitSprint {  get; set; } = false;

        public static bool shopIsFree { get; set; } = false;

        // for testing wiht a button (doesn't work yet)
        public static KeyCode freeCamButton { get; set; } = KeyCode.F;

        public static bool freeCam {  get; set; } = false;
    }
}
