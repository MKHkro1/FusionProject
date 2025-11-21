using System.Collections;
using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Unity.VisualScripting;
using UnityEngine;

namespace ZombieBoss2Remake.BepInEx;

public class BlackHole : MonoBehaviour
{
    public BlackHole() : base(ClassInjector.DerivedConstructorPointer<BlackHole>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public BlackHole(IntPtr i) : base(i)
    {
    }

    public Transform? Cloud { get; set; }
    public global::BlackHole? placeHolder { get; set; }

    public void Start()
    {
        Cloud = gameObject.transform.FindChild("cloud");
        placeHolder = gameObject.AddComponent<global::BlackHole>();
        placeHolder.enabled = false;
        placeHolder.gold = true;
        this.StartCoroutine(Explode());
    }

    public void FixedUpdate()
    {
        var radius = 18;

        // 检测范围内的子弹
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            transform.position,
            radius,
            LayerMask.GetMask("Bullet")
        );

        foreach (var col in colliders)
            if (col.TryGetComponent(out Bullet bullet))
            {
                bullet.PostionUpdate();
                // 修改子弹状态
                bullet.theStatus = (BulletStatus)2;
                bullet.theMovingWay = 99;
                // 计算方向向量
                var direction = transform.position - bullet.transform.position;
                Vector2 normalizedDir = direction.normalized;

                // 计算距离相关参数
                var distance = direction.magnitude;
                var tangent = new Vector2(-direction.y, direction.x).normalized * 0.5f;
                var distanceFactor = radius * 18.0f / (distance * distance);
                var attractionForce = normalizedDir * distanceFactor + tangent;

                // 应用物理效果
                var rb = bullet.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    var newVelocity = rb.velocity + attractionForce * Time.fixedDeltaTime;
                    bullet.GetComponent<Rigidbody2D>().velocity = newVelocity;
                }

                var absorbThreshold = transform.localScale.x * 0.5f + 0.5f;
                if (distance < absorbThreshold)
                    // 吸收子弹
                    bullet.Die();
            }
    }

    [HideFromIl2Cpp]
    public IEnumerator Explode()
    {
        var m = 0;
        for (var i = 0; i < 600; i++)
        {
            if (gameObject is null || gameObject.IsDestroyed()) yield break;
            Cloud?.Rotate(0, 0, -5);
            if (m % 3 is 0)
                foreach (var p in Board.Instance.plantArray)
                    if (p is not null && p.thePlantHealth > 40)
                    {
                        p.thePlantHealth -= 3;
                        p.UpdateText();
                    }

            m++;
            yield return new WaitForSeconds(Time.deltaTime);
        }

        for (var j = 0; j < 10; j++)
        {
            if (gameObject is null || gameObject.IsDestroyed()) yield break;
            transform.localScale *= 0.8f;
            yield return new WaitForSeconds(Time.deltaTime);
        }

        if (gameObject is null || gameObject.IsDestroyed() || Board.Instance is null ||
            Board.Instance.IsDestroyed()) yield break;
        foreach (var p in Board.Instance.plantArray)
            if (p is not null)
            {
                p.thePlantMaxHealth = p.thePlantHealth;
                p.UpdateText();
                //Board.Instance.SetDoom(p.thePlantColumn, p.thePlantRow, false, damage: 0);
                CreateParticle.SetParticle(98, p.transform.position, p.thePlantRow);
                GameAPP.PlaySound(41, 1.8f);
            }

        ScreenShake.TriggerShake(0.4f);
        for (var j = Board.Instance.plantArray.Count - 1; j >= 0; j--)
            if (Board.Instance.plantArray[j] is not null &&
                Board.Instance.plantArray[j].thePlantType is PlantType.GoldMelon or PlantType.SuperSunNut)
                Board.Instance.plantArray[j].Die();

        gameObject.active = false;
        Destroy(gameObject);
    }
}