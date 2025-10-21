using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace CherryBigGatling.BepInEx
{
    public class CherryBigGatling : MonoBehaviour
    {
        public CherryBigGatling() : base(ClassInjector.DerivedConstructorPointer<CherryBigGatling>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        public CherryBigGatling(IntPtr i) : base(i)
        {
        }

        public void AnimRaised()
        {
            bool flag = this.plant != null;
            if (flag)
            {
                this.plant.theStatus = (PlantStatus)8;
            }
        }

        public void AnimShoot()
        {
            bool flag = this.plant.thePlantType == (PlantType)2005;
            if (flag)
            {
                bool flag2 = this.plant.theStatus != (PlantStatus)8;
                if (!flag2)
                {
                    Vector3 position = this.plant.shoot.transform.position;
                    Console.WriteLine($"Spawning SnowPea bullet at {position.x}, {position.y} with type {3}");
                    CreateBullet.Instance.SetBullet(position.x, position.y - 0.3f, this.plant.thePlantRow, (BulletType)3, 0, false).Damage = 500;
                    CreateBullet.Instance.SetBullet(position.x, position.y, this.plant.thePlantRow, (BulletType)3, 0, false).Damage = 500;
                    CreateBullet.Instance.SetBullet(position.x, position.y + 0.3f, this.plant.thePlantRow, (BulletType)3, 0, false).Damage = 500;
                }
            }
        }

        public void Awake()
        {
            this.plant.shoot = this.plant.gameObject.transform.GetChild(0).GetChild(3);
            Plant.PlantTag plantTag = this.plant.plantTag;
            plantTag.doubleBoxPlant = true;
            this.plant.plantTag = plantTag;
            this.plant.isConnected = true;
            this.plant.theStatus = (PlantStatus)8;
            this.plant.attackSpeed = 0f;
            this.plant.attackDamage = 500;
        }

        public BigGatling plant
        {
            get
            {
                return base.gameObject.GetComponent<BigGatling>();
            }
        }
    }
}
