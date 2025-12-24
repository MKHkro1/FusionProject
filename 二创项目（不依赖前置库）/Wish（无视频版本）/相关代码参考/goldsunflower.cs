/*
植物机制：
更快的产生阳光。
特点：每10秒累计50阳光，最多一万，关卡内阳光上限调整为5万，溢出的部分将存到可用的金向日葵中
升级消耗：3000*2^场上金向日葵数量
大招：消耗1000钱币，使全屏的金向日葵释放全部阳光
融合配方：向日葵+豌豆射手

修正图鉴
飞快生产阳光并储存的向日葵，大招取出阳光。
阳光产量：50/10秒
特点：生产的阳光会储存起来，每株最多储存1万。超过上限的阳光会存储在最新的金向日葵内
大招：消耗1000金钱，冷却0秒。使全场金向日葵释放储存的阳光
升级消耗：3000金钱，每有一个金向日葵消耗翻倍
*/

void GoldSunflower__AttributeEvent(GoldSunflower_o *this, const MethodInfo *method)
{
  const MethodInfo *method_1; // r8
  int32_t attributeCount; // eax

  System_Configuration_ConfigurationCollectionAttribute___ctor(
    (System_Configuration_ConfigurationCollectionAttribute_o *)this,
    0,
    method_1);
  attributeCount = this->fields.attributeCount + 1;
  this->fields.attributeCountdown = 10.0;
  this->fields.attributeCount = attributeCount;
  if ( attributeCount > 200 )
    this->fields.attributeCount = 200;
}

bool GoldSunflower__SuperSkill(GoldSunflower_o *this, const MethodInfo *method)
{
  UnityEngine_Transform_o *axis; // rdx
  UnityEngine_Vector3_o *position; // rax
  __m128 v5; // xmm3
  __m128 y_low; // xmm2
  float z; // xmm1_4
  int32_t thePlantRow; // r8d
  __int64 v9; // r9
  __int64 v10; // rdx
  __int64 v11; // rdx
  __int64 routine; // rbx
  const MethodInfo *method_2; // r8
  int32_t originalRow; // r8d
  int32_t newColumn; // r9d
  const MethodInfo *method_1; // [rsp+20h] [rbp-48h]
  const MethodInfo *method_3; // [rsp+28h] [rbp-40h]
  UnityEngine_Vector3_o position_1[2]; // [rsp+30h] [rbp-38h] BYREF

  if ( !byte_1820C26A8 )
  {
    sub_180296CC0(&GameAPP_TypeInfo, method);
    byte_1820C26A8 = 1;
  }
  if ( this->fields.flashCountDown > 0.0 )
    return 0;
  axis = this->fields.axis;
  if ( !axis )
    sub_180296EF0(this, 0);
  position = UnityEngine_Transform__get_position(position_1, axis, 0);
  v5 = (__m128)*(unsigned __int64 *)&position->fields.x;
  y_low = (__m128)LODWORD(position->fields.y);
  v5.m128_f32[0] = v5.m128_f32[0] + 0.0;
  y_low.m128_f32[0] = y_low.m128_f32[0] + 0.75;
  z = position->fields.z + 0.0;
  thePlantRow = this->fields.thePlantRow;
  *(_QWORD *)&position_1[0].fields.x = _mm_unpacklo_ps(v5, y_low).m128_u64[0];
  position_1[0].fields.z = z;
  CreateParticle__SetParticle(69, position_1, thePlantRow, 1, 0);
  LOBYTE(v9) = 1;
  method_1 = this->klass->vtable._28_Recover.method;
  ((void (__fastcall *)(GoldSunflower_o *, Il2CppMethodPointer, _QWORD, __int64))this->klass->vtable._28_Recover.methodPtr)(
    this,
    this->klass->vtable._28_Recover.methodPtr,
    0,
    v9);
  if ( !GameAPP_TypeInfo->_2.cctor_finished )
    il2cpp_runtime_class_init(GameAPP_TypeInfo, v10);
  GameAPP__PlaySound(66, 0.5, 1.0, 0);
  if ( !byte_1820C26A9 )
  {
    sub_180296CC0(&GoldSunflower__ContinueProduce_d__3_TypeInfo, v11);
    byte_1820C26A9 = 1;
  }
  routine = sub_180245400(GoldSunflower__ContinueProduce_d__3_TypeInfo, v11);
  System_Configuration_ConfigurationCollectionAttribute___ctor(
    (System_Configuration_ConfigurationCollectionAttribute_o *)routine,
    0,
    method_2);
  *(_DWORD *)(routine + 16) = 0;
  *(_QWORD *)(routine + 32) = this;
  sub_180296050((HypnoPumpkin_o *)(routine + 32), (int32_t)this, originalRow, newColumn, (int32_t)method_1, method_3);
  UnityEngine_MonoBehaviour__StartCoroutine_Auto(
    (UnityEngine_MonoBehaviour_o *)this,
    (System_Collections_IEnumerator_o *)routine,
    0);
  this->fields.flashCountDown = (float)this->fields.attributeCount * 0.1;
  return 1;
}

bool GoldSunflower__ContinueProduce_d__3__MoveNext(
        GoldSunflower__ContinueProduce_d__3_o *this,
        const MethodInfo *method)
{
  GoldSunflower__ContinueProduce_d__3_o *this_1; // rdi
  __int64 v3; // rdx
  __int64 v4; // rdx
  __int64 v5; // rdx
  int32_t *_4__this; // rbx
  __int64 v7; // rdx
  int32_t theSoundID; // esi
  GoldSunflower__ContinueProduce_d__3_o **static_fields; // rcx
  int32_t theRow; // r8d
  UnityEngine_GameObject_o *v11; // rax
  Il2CppObject *Component_object; // rax
  UnityEngine_WaitForSeconds_o *__2__current; // rbx
  int32_t originalRow; // r8d
  int32_t newColumn; // r9d
  int32_t newRow; // [rsp+20h] [rbp-38h]
  const MethodInfo *method_1; // [rsp+28h] [rbp-30h]
  UnityEngine_Vector3_o pos_; // [rsp+40h] [rbp-18h] BYREF

  this_1 = this;
  if ( !byte_1820C26AA )
  {
    sub_180296CC0(&CreateItem_TypeInfo, method);
    sub_180296CC0(&GameAPP_TypeInfo, v3);
    sub_180296CC0(&Method_UnityEngine_GameObject_GetComponent_CoinSun___, v4);
    sub_180296CC0(&UnityEngine_WaitForSeconds_TypeInfo, v5);
    byte_1820C26AA = 1;
  }
  _4__this = (int32_t *)this_1->fields.__4__this;
  if ( this_1->fields.__1__state <= 1u )
  {
    this_1->fields.__1__state = -1;
    if ( !_4__this )
      goto LABEL_13;
    if ( _4__this[60] > 0 )
    {
      theSoundID = UnityEngine_Random__RandomRangeInt(3, 5, 0);
      if ( !GameAPP_TypeInfo->_2.cctor_finished )
        il2cpp_runtime_class_init(GameAPP_TypeInfo, v7);
      GameAPP__PlaySound(theSoundID, 0.30000001, 1.0, 0);
      static_fields = (GoldSunflower__ContinueProduce_d__3_o **)CreateItem_TypeInfo->static_fields;
      *(_QWORD *)&pos_.fields.x = 0;
      this = *static_fields;
      if ( this )
      {
        theRow = _4__this[55];
        pos_.fields.z = 0.0;
        v11 = CreateItem__SetCoin((CreateItem_o *)this, _4__this[54], theRow, 1, 0, &pos_, 0, 0);
        if ( v11 )
        {
          Component_object = UnityEngine_GameObject__GetComponent_object_(
                               v11,
                               Method_UnityEngine_GameObject_GetComponent_CoinSun___);
          if ( Component_object )
          {
            BYTE4(Component_object[2].monitor) = 0;
            --_4__this[60];
            __2__current = (UnityEngine_WaitForSeconds_o *)sub_180245400(UnityEngine_WaitForSeconds_TypeInfo, method);
            UnityEngine_WaitForSeconds___ctor(__2__current, 0.1, 0);
            this_1->fields.__2__current = (Il2CppObject *)__2__current;
            sub_180296050(
              (HypnoPumpkin_o *)&this_1->fields.__2__current,
              (int32_t)__2__current,
              originalRow,
              newColumn,
              newRow,
              method_1);
            this_1->fields.__1__state = 1;
            return 1;
          }
        }
      }
LABEL_13:
      sub_180296EF0(this, method);
    }
  }
  return 0;
}

void __noreturn GoldSunflower__ContinueProduce_d__3__System_Collections_IEnumerator_Reset(
        GoldSunflower__ContinueProduce_d__3_o *this,
        const MethodInfo *method)
{
  __int64 v2; // rax
  __int64 v3; // rdx
  System_NotSupportedException_o *v4; // rbx
  __int64 v5; // rdx
  __int64 v6; // rax

  v2 = sub_180296CE0(&System_NotSupportedException_TypeInfo, method);
  v4 = (System_NotSupportedException_o *)sub_180245400(v2, v3);
  System_NotSupportedException___ctor(v4, 0);
  v6 = sub_180296CE0(&Method_GoldSunflower__ContinueProduce_d__3_System_Collections_IEnumerator_Reset__, v5);
  sub_180296EB0(v4, v6);
}