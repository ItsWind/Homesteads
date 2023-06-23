using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models {
    public class HomesteadSceneSavedEntity {
        [SaveableField(1)]
        public HomesteadScenePlaceable Placeable;
        [SaveableField(2)]
        public float posX;
        [SaveableField(3)]
        public float posY;
        [SaveableField(4)]
        public float posZ;
        [SaveableField(5)]
        public float rotFx;
        [SaveableField(6)]
        public float rotFy;
        [SaveableField(7)]
        public float rotFz;
        [SaveableField(8)]
        public float rotUx;
        [SaveableField(9)]
        public float rotUy;
        [SaveableField(10)]
        public float rotUz;
        [SaveableField(11)]
        public float rotSx;
        [SaveableField(12)]
        public float rotSy;
        [SaveableField(13)]
        public float rotSz;

        public HomesteadSceneSavedEntity(HomesteadScenePlaceable placeable, Vec3 position, Mat3 rotation) {
            Placeable = placeable;
            posX = position.x;
            posY = position.y;
            posZ = position.z;
            rotFx = rotation.f.x;
            rotFy = rotation.f.y;
            rotFz = rotation.f.z;
            rotUx = rotation.u.x;
            rotUy = rotation.u.y;
            rotUz = rotation.u.z;
            rotSx = rotation.s.x;
            rotSy = rotation.s.y;
            rotSz = rotation.s.z;
        }
    }
}
