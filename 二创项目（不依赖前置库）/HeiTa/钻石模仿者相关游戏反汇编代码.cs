void DiamondImitater__AnimExplode(DiamondImitater_o *this, const MethodInfo *method)
{
  __int64 v3; // rdx
  __int64 v4; // rdx
  __int64 v5; // rdx
  __int64 v6; // rdx
  __int64 v7; // rdx
  __int64 v8; // rdx
  __int64 v9; // rdx
  __int64 v10; // rdx
  __int64 v11; // rdx
  __int64 v12; // rdx
  __int64 v13; // rdx
  __int64 v14; // rdx
  __int64 v15; // rdx
  __int64 v16; // rdx
  __int64 v17; // rdx
  __int64 v18; // rdx
  __int64 v19; // rdx
  __int64 v20; // rdx
  __int64 v21; // rdx
  __int64 v22; // rdx
  __int64 v23; // rdx
  __int64 v24; // rdx
  __int64 v25; // rdx
  __int64 v26; // rdx
  DiamondImitater_c *klass; // r8
  unsigned int v28; // edi
  int32_t n10; // eax
  UnityEngine_Transform_o *axis; // rdx
  CreatePlant_o *static_fields; // rcx
  __int64 v32; // r8
  System_Collections_Generic_List_RegexCharClass_SingleRange__o *UltimatePlants; // r15
  DiamondImitater___c_c *DiamondImitater.__c_TypeInfo; // rax
  System_Console_WindowsConsole_WindowsCancelHandler_o *monitor; // r14
  Il2CppObject *_; // rbx
  int32_t thePlantColumn; // r14d
  int32_t thePlantRow_2; // r12d
  CreatePlant_o *Instance_2; // rbx
  int32_t index; // eax
  System_Text_RegularExpressions_RegexCharClass_SingleRange_o Item; // eax
  ParticleManager_o *klass_1; // rbx
  UnityEngine_Vector3_o *position_2; // rax
  __m128 v44; // xmm3
  __m128 y_low; // xmm2
  struct CreatePlant_StaticFields *static_fields_1; // rcx
  UnityEngine_GameObject_o *Plant__SetPlant; // rax
  Il2CppObject *v48; // rax
  CreateZombie_c *CreateZombie_TypeInfo_2; // rax
  int32_t thePlantRow_3; // r14d
  CreateZombie_o *Instance_3; // rbx
  UnityEngine_Vector3_o *position_3; // rax
  UnityEngine_GameObject_o *v53; // rax
  const MethodInfo_69CCB0 *Method$UnityEngine.GameObject.GetComponent_RandomZombie_(); // rdx
  CreateZombie_c *CreateZombie_TypeInfo_1; // rax
  int32_t thePlantRow_1; // r14d
  CreateZombie_o *Instance_1; // rbx
  UnityEngine_Vector3_o *position_1; // rax
  Il2CppObject *Component_object; // rax
  Il2CppType *DiamondImitater.RandomEffect_var; // rbx
  __int64 v61; // rdx
  System_Type_o *TypeFromHandle; // rbx
  System_Array_o *Values; // rax
  const MethodInfo_678D10 *Method$System.Linq.Enumerable.ToList_DiamondImitater.RandomEffe; // r15
  System_Array_o *Values_1; // rbx
  System_Collections_Generic_IEnumerable_TSource__o *source; // rax
  System_Collections_Generic_List_TSource__o *v67; // rax
  System_Collections_Generic_List_RegexCharClass_SingleRange__o *v68; // rbx
  int32_t index_1; // eax
  System_Text_RegularExpressions_RegexCharClass_SingleRange_o Item_1; // eax
  int v71; // eax
  int v72; // eax
  int v73; // eax
  UnityEngine_Vector3_o *position_4; // rax
  __m128 v75; // xmm7
  __m128 v76; // xmm6
  __int64 v77; // rdx
  UnityEngine_LayerMask_o v78; // ebx
  UnityEngine_Collider2D_array *v79; // rax
  UnityEngine_Collider2D_array *v80; // r13
  unsigned int static_fields_3; // r14d
  UnityEngine_Component_o **m_Items; // r15
  UnityEngine_Component_o *v83; // rbx
  UnityEngine_LayerMask_o v84; // eax
  __int64 v85; // rdx
  int32_t theSoundID; // ebx
  ParticleManager_o *klass_2; // rbx
  UnityEngine_Vector3_o *position_5; // rax
  __m128 v89; // xmm2
  __m128 v90; // xmm1
  UnityEngine_Vector3_o *position_6; // rax
  __m128 v92; // xmm7
  __m128 v93; // xmm6
  __int64 v94; // rdx
  UnityEngine_LayerMask_o v95; // ebx
  UnityEngine_Collider2D_array *v96; // r14
  int v97; // eax
  CreatePlant_o **m_Items_1; // rbx
  int v99; // ebx
  struct CreateItem_StaticFields *static_fields_2; // rcx
  int32_t thePlantRow_4; // r8d
  int32_t thePlantColumn_1; // edx
  CreateZombie_c *CreateZombie_TypeInfo_3; // rax
  int32_t thePlantRow_5; // r14d
  CreateZombie_o *Instance_4; // rbx
  UnityEngine_Vector3_o *position_7; // rax
  UnityEngine_GameObject_o *v107; // rax
  Il2CppObject *static_fields_5; // rax
  CreateZombie_c *CreateZombie_TypeInfo; // rax
  int32_t thePlantRow; // r14d
  CreateZombie_o *Instance; // rbx
  UnityEngine_Vector3_o *position; // rax
  UnityEngine_GameObject_o *Zombie__SetZombie; // rax
  UnityEngine_Vector3_o pos_; // [rsp+50h] [rbp-39h] BYREF
  UnityEngine_Vector3_o v115; // [rsp+60h] [rbp-29h] BYREF
  Il2CppObject *component; // [rsp+F0h] [rbp+67h] BYREF
  Il2CppObject *static_fields_4; // [rsp+100h] [rbp+77h] BYREF
  UnityEngine_Vector2_o puffV; // [rsp+108h] [rbp+7Fh]

  if ( !byte_1820C2499 )
  {
    sub_180296CC0(&Method_UnityEngine_Component_TryGetComponent_DiamondRandomZombie___, method);
    sub_180296CC0(&Method_UnityEngine_Component_TryGetComponent_Plant___, v3);
    sub_180296CC0(&CreateItem_TypeInfo, v4);
    sub_180296CC0(&CreatePlant_TypeInfo, v5);
    sub_180296CC0(&CreateZombie_TypeInfo, v6);
    sub_180296CC0(&System_Enum_TypeInfo, v7);
    sub_180296CC0(&Method_System_Linq_Enumerable_ToList_DiamondImitater_RandomEffect___, v8);
    sub_180296CC0(&GameAPP_TypeInfo, v9);
    sub_180296CC0(&Method_UnityEngine_GameObject_GetComponent_DiamondRandomZombie___, v10);
    sub_180296CC0(&Method_UnityEngine_GameObject_GetComponent_Present___, v11);
    sub_180296CC0(&Method_UnityEngine_GameObject_GetComponent_RandomZombie___, v12);
    sub_180296CC0(&Lawnf_TypeInfo, v13);
    sub_180296CC0(&Method_System_Collections_Generic_List_PlantType__RemoveAll__, v14);
    sub_180296CC0(&Method_System_Collections_Generic_List_PlantType__get_Count__, v15);
    sub_180296CC0(&Method_System_Collections_Generic_List_DiamondImitater_RandomEffect__get_Count__, v16);
    sub_180296CC0(&Method_System_Collections_Generic_List_DiamondImitater_RandomEffect__get_Item__, v17);
    sub_180296CC0(&Method_System_Collections_Generic_List_PlantType__get_Item__, v18);
    sub_180296CC0(&ParticleManager_TypeInfo, v19);
    sub_180296CC0(&UnityEngine_Physics2D_TypeInfo, v20);
    sub_180296CC0(&System_Predicate_PlantType__TypeInfo, v21);
    sub_180296CC0(&DiamondImitater_RandomEffect___TypeInfo, v22);
    sub_180296CC0(&DiamondImitater_RandomEffect_var, v23);
    sub_180296CC0(&System_Type_TypeInfo, v24);
    sub_180296CC0(&Method_DiamondImitater___c__AnimExplode_b__0_0__, v25);
    sub_180296CC0(&DiamondImitater___c_TypeInfo, v26);
    byte_1820C2499 = 1;
  }
  klass = this->klass;
  v28 = 0;
  static_fields_4 = 0;
  component = 0;
  ((void (__fastcall *)(DiamondImitater_o *, __int64, const MethodInfo *))klass->vtable._20_Die.methodPtr)(
    this,
    2,
    klass->vtable._20_Die.method);
  n10 = UnityEngine_Random__RandomRangeInt(0, 100, 0);
  if ( n10 < 10 )
  {
    CreateZombie_TypeInfo = CreateZombie_TypeInfo;
    if ( !CreateZombie_TypeInfo->_2.cctor_finished )
    {
      il2cpp_runtime_class_init(CreateZombie_TypeInfo, axis);
      CreateZombie_TypeInfo = CreateZombie_TypeInfo;
    }
    axis = this->fields.axis;
    thePlantRow = this->fields.thePlantRow;
    Instance = CreateZombie_TypeInfo->static_fields->Instance;
    if ( !axis )
      goto LABEL_110;
    position = UnityEngine_Transform__get_position(&v115, axis, 0);
    if ( !Instance )
      goto LABEL_110;
    Zombie__SetZombie = CreateZombie__SetZombie(Instance, thePlantRow, 215, position->fields.x, 0, 0);
    if ( !Zombie__SetZombie )
      goto LABEL_110;
    Component_object = UnityEngine_GameObject__GetComponent_object_(
                         Zombie__SetZombie,
                         Method_UnityEngine_GameObject_GetComponent_DiamondRandomZombie___);
    if ( !Component_object )
      goto LABEL_110;
    LOBYTE(Component_object[40].klass) = 1;
    goto LABEL_38;
  }
  if ( n10 >= 15 )
  {
    if ( n10 < 30 )
    {
      CreateZombie_TypeInfo_1 = CreateZombie_TypeInfo;
      if ( !CreateZombie_TypeInfo->_2.cctor_finished )
      {
        il2cpp_runtime_class_init(CreateZombie_TypeInfo, axis);
        CreateZombie_TypeInfo_1 = CreateZombie_TypeInfo;
      }
      axis = this->fields.axis;
      thePlantRow_1 = this->fields.thePlantRow;
      Instance_1 = CreateZombie_TypeInfo_1->static_fields->Instance;
      if ( !axis )
        goto LABEL_110;
      position_1 = UnityEngine_Transform__get_position(&v115, axis, 0);
      if ( !Instance_1 )
        goto LABEL_110;
      v53 = CreateZombie__SetZombie(Instance_1, thePlantRow_1, 110, position_1->fields.x, 0, 0);
      if ( !v53 )
        goto LABEL_110;
      Method$UnityEngine.GameObject.GetComponent_RandomZombie_() = Method_UnityEngine_GameObject_GetComponent_RandomZombie___;
    }
    else
    {
      if ( n10 >= 40 )
      {
        if ( n10 >= 90 )
        {
          if ( !Lawnf_TypeInfo->_2.cctor_finished )
            il2cpp_runtime_class_init(Lawnf_TypeInfo, axis);
          UltimatePlants = (System_Collections_Generic_List_RegexCharClass_SingleRange__o *)Lawnf__GetUltimatePlants(0);
          DiamondImitater.__c_TypeInfo = DiamondImitater___c_TypeInfo;
          if ( !DiamondImitater___c_TypeInfo->_2.cctor_finished )
          {
            il2cpp_runtime_class_init(DiamondImitater___c_TypeInfo, axis);
            DiamondImitater.__c_TypeInfo = DiamondImitater___c_TypeInfo;
          }
          static_fields = (CreatePlant_o *)DiamondImitater.__c_TypeInfo->static_fields;
          monitor = (System_Console_WindowsConsole_WindowsCancelHandler_o *)static_fields->monitor;
          if ( !monitor )
          {
            if ( !DiamondImitater.__c_TypeInfo->_2.cctor_finished )
            {
              il2cpp_runtime_class_init(DiamondImitater.__c_TypeInfo, axis);
              DiamondImitater.__c_TypeInfo = DiamondImitater___c_TypeInfo;
            }
            _ = (Il2CppObject *)DiamondImitater.__c_TypeInfo->static_fields->__9;
            monitor = (System_Console_WindowsConsole_WindowsCancelHandler_o *)sub_180245400(
                                                                                System_Predicate_PlantType__TypeInfo,
                                                                                axis);
            System_Console_WindowsConsole_WindowsCancelHandler___ctor(
              monitor,
              _,
              Method_DiamondImitater___c__AnimExplode_b__0_0__,
              0);
            DiamondImitater___c_TypeInfo->static_fields->__9__0_0 = (struct System_Predicate_PlantType__o *)monitor;
            sub_180296050(&DiamondImitater___c_TypeInfo->static_fields->__9__0_0, monitor);
          }
          if ( UltimatePlants )
          {
            System_Collections_Generic_List_RegexCharClass_SingleRange___RemoveAll(
              UltimatePlants,
              (System_Predicate_T__o *)monitor,
              Method_System_Collections_Generic_List_PlantType__RemoveAll__);
            thePlantColumn = this->fields.thePlantColumn;
            thePlantRow_2 = this->fields.thePlantRow;
            Instance_2 = CreatePlant_TypeInfo->static_fields->Instance;
            index = UnityEngine_Random__RandomRangeInt(0, UltimatePlants->fields._size, 0);
            Item = System_Collections_Generic_List_RegexCharClass_SingleRange___get_Item(
                     UltimatePlants,
                     index,
                     Method_System_Collections_Generic_List_PlantType__get_Item__);
            static_fields = 0;
            puffV = 0;
            if ( Instance_2 )
            {
              CreatePlant__SetPlant(Instance_2, thePlantColumn, thePlantRow_2, *(_DWORD *)&Item, 0, puffV, 1, 0, 0, 0);
              axis = this->fields.axis;
              static_fields = (CreatePlant_o *)ParticleManager_TypeInfo->static_fields;
              klass_1 = (ParticleManager_o *)static_fields->klass;
              if ( axis )
              {
                position_2 = UnityEngine_Transform__get_position(&v115, axis, 0);
                v44 = (__m128)*(unsigned __int64 *)&position_2->fields.x;
                y_low = (__m128)LODWORD(position_2->fields.y);
                v44.m128_f32[0] = v44.m128_f32[0] + 0.0;
                y_low.m128_f32[0] = y_low.m128_f32[0] + 0.5;
                if ( klass_1 )
                {
                  ParticleManager__SetParticle(
                    klass_1,
                    11,
                    (UnityEngine_Vector2_o)*(_OWORD *)&_mm_unpacklo_ps(v44, y_low),
                    this->fields.thePlantRow,
                    0);
                  return;
                }
              }
            }
          }
LABEL_110:
          sub_180296EF0(static_fields, axis, v32);
        }
        static_fields_1 = CreatePlant_TypeInfo->static_fields;
        puffV = 0;
        static_fields = static_fields_1->Instance;
        if ( !static_fields )
          goto LABEL_110;
        Plant__SetPlant = CreatePlant__SetPlant(
                            static_fields,
                            this->fields.thePlantColumn,
                            this->fields.thePlantRow,
                            256,
                            0,
                            puffV,
                            1,
                            0,
                            0,
                            0);
        if ( !Plant__SetPlant )
          goto LABEL_110;
        v48 = UnityEngine_GameObject__GetComponent_object_(
                Plant__SetPlant,
                Method_UnityEngine_GameObject_GetComponent_Present___);
        if ( !v48 )
          goto LABEL_110;
        ((void (__fastcall *)(Il2CppObject *, const MethodInfo *))v48->klass->vtable[46].methodPtr)(
          v48,
          v48->klass->vtable[46].method);
        return;
      }
      CreateZombie_TypeInfo_2 = CreateZombie_TypeInfo;
      if ( !CreateZombie_TypeInfo->_2.cctor_finished )
      {
        il2cpp_runtime_class_init(CreateZombie_TypeInfo, axis);
        CreateZombie_TypeInfo_2 = CreateZombie_TypeInfo;
      }
      axis = this->fields.axis;
      thePlantRow_3 = this->fields.thePlantRow;
      Instance_3 = CreateZombie_TypeInfo_2->static_fields->Instance;
      if ( !axis )
        goto LABEL_110;
      position_3 = UnityEngine_Transform__get_position(&v115, axis, 0);
      if ( !Instance_3 )
        goto LABEL_110;
      v53 = CreateZombie__SetZombie(Instance_3, thePlantRow_3, 215, position_3->fields.x, 0, 0);
      if ( !v53 )
        goto LABEL_110;
      Method$UnityEngine.GameObject.GetComponent_RandomZombie_() = Method_UnityEngine_GameObject_GetComponent_DiamondRandomZombie___;
    }
    Component_object = UnityEngine_GameObject__GetComponent_object_(
                         v53,
                         Method$UnityEngine.GameObject.GetComponent_RandomZombie_());
    if ( !Component_object )
      goto LABEL_110;
LABEL_38:
    LODWORD(Component_object[9].klass) = 1;
    ((void (__fastcall *)(Il2CppObject *, _QWORD, __int64, _QWORD, const MethodInfo *))Component_object->klass->vtable[18].methodPtr)(
      Component_object,
      0,
      1,
      0,
      Component_object->klass->vtable[18].method);
    return;
  }
  DiamondImitater.RandomEffect_var = DiamondImitater_RandomEffect_var;
  if ( !System_Type_TypeInfo->_2.cctor_finished )
    il2cpp_runtime_class_init(System_Type_TypeInfo, axis);
  TypeFromHandle = System_Type__GetTypeFromHandle((System_RuntimeTypeHandle_o)DiamondImitater.RandomEffect_var, 0);
  if ( !System_Enum_TypeInfo->_2.cctor_finished )
    il2cpp_runtime_class_init(System_Enum_TypeInfo, v61);
  Values = System_Enum__GetValues(TypeFromHandle, 0);
  Method$System.Linq.Enumerable.ToList_DiamondImitater.RandomEffe = Method_System_Linq_Enumerable_ToList_DiamondImitater_RandomEffect___;
  Values_1 = Values;
  if ( Values )
  {
    source = (System_Collections_Generic_IEnumerable_TSource__o *)sub_180296070(
                                                                    Values,
                                                                    DiamondImitater_RandomEffect___TypeInfo);
    if ( !source )
      sub_180296090(Values_1);
  }
  else
  {
    source = 0;
  }
  v67 = System_Linq_Enumerable__ToList_Int32Enum_(
          source,
          Method$System.Linq.Enumerable.ToList_DiamondImitater.RandomEffe);
  v68 = (System_Collections_Generic_List_RegexCharClass_SingleRange__o *)v67;
  if ( !v67 )
    goto LABEL_110;
  index_1 = UnityEngine_Random__RandomRangeInt(0, v67->fields._size, 0);
  Item_1 = System_Collections_Generic_List_RegexCharClass_SingleRange___get_Item(
             v68,
             index_1,
             Method_System_Collections_Generic_List_DiamondImitater_RandomEffect__get_Item__);
  if ( Item_1 )
  {
    v71 = *(_DWORD *)&Item_1 - 1;
    if ( !v71 )
    {
      v99 = 0;
      while ( 1 )
      {
        static_fields_2 = CreateItem_TypeInfo->static_fields;
        *(_QWORD *)&v115.fields.x = 0;
        static_fields = (CreatePlant_o *)static_fields_2->Instance;
        if ( !static_fields )
          break;
        thePlantRow_4 = this->fields.thePlantRow;
        thePlantColumn_1 = this->fields.thePlantColumn;
        pos_.fields.z = 0.0;
        *(_QWORD *)&pos_.fields.x = *(_QWORD *)&v115.fields.x;
        CreateItem__SetCoin((CreateItem_o *)static_fields, thePlantColumn_1, thePlantRow_4, 0, 0, &pos_, 0, 0);
        if ( ++v99 >= 8 )
          return;
      }
      goto LABEL_110;
    }
    v72 = v71 - 1;
    if ( v72 )
    {
      v73 = v72 - 1;
      if ( v73 )
      {
        if ( v73 == 1 )
        {
          axis = this->fields.axis;
          if ( !axis )
            goto LABEL_110;
          position_4 = UnityEngine_Transform__get_position(&v115, axis, 0);
          v75 = (__m128)*(unsigned __int64 *)&position_4->fields.x;
          v76 = (__m128)LODWORD(position_4->fields.y);
          v75.m128_f32[0] = v75.m128_f32[0] + 0.0;
          v76.m128_f32[0] = v76.m128_f32[0] + 0.5;
          v78.fields.m_Mask = UnityEngine_LayerMask__op_Implicit(this->fields.plantLayer.fields.m_Mask, 0).fields.m_Mask;
          if ( !UnityEngine_Physics2D_TypeInfo->_2.cctor_finished )
            il2cpp_runtime_class_init(UnityEngine_Physics2D_TypeInfo, v77);
          v79 = UnityEngine_Physics2D__OverlapCircleAll_6466051776(
                  (UnityEngine_Vector2_o)*(_OWORD *)&_mm_unpacklo_ps(v75, v76),
                  2.0,
                  v78.fields.m_Mask,
                  0);
          v80 = v79;
          static_fields_3 = 0;
          static_fields = 0;
          if ( !v79 )
            goto LABEL_110;
          m_Items = (UnityEngine_Component_o **)v79->m_Items;
          while ( (int)static_fields < SLODWORD(v80->max_length) )
          {
            if ( static_fields_3 >= LODWORD(v80->max_length) )
              goto LABEL_111;
            v83 = *m_Items;
            if ( !*m_Items )
              goto LABEL_110;
            if ( UnityEngine_Component__TryGetComponent_object_(
                   *m_Items,
                   &component,
                   Method_UnityEngine_Component_TryGetComponent_Plant___) )
            {
              v84.fields.m_Mask = UnityEngine_Collider2D__get_contactCaptureLayers((UnityEngine_Collider2D_o *)v83, 0).fields.m_Mask;
              if ( UnityEngine_LayerMask__op_Implicit(v84.fields.m_Mask, 0).fields.m_Mask )
              {
                static_fields = (CreatePlant_o *)component;
                if ( !component )
                  goto LABEL_110;
                if ( (int)sub_180344E80((unsigned int)(HIDWORD(component[13].monitor) - this->fields.thePlantRow), 0) <= 1 )
                {
                  theSoundID = UnityEngine_Random__RandomRangeInt(0, 3, 0);
                  if ( !GameAPP_TypeInfo->_2.cctor_finished )
                    il2cpp_runtime_class_init(GameAPP_TypeInfo, v85);
                  GameAPP__PlaySound(theSoundID, 0.5, 1.0, 0);
                  static_fields = (CreatePlant_o *)component;
                  if ( !component )
                    goto LABEL_110;
                  ((void (__fastcall *)(Il2CppObject *, __int64, _QWORD, const MethodInfo *))component->klass->vtable[16].methodPtr)(
                    component,
                    4000,
                    0,
                    component->klass->vtable[16].method);
                  static_fields = (CreatePlant_o *)component;
                  if ( !component )
                    goto LABEL_110;
                  Plant__FlashOnce((Plant_o *)component, 0);
                  static_fields = (CreatePlant_o *)ParticleManager_TypeInfo->static_fields;
                  klass_2 = (ParticleManager_o *)static_fields->klass;
                  if ( !component )
                    goto LABEL_110;
                  axis = (UnityEngine_Transform_o *)component[3].monitor;
                  if ( !axis )
                    goto LABEL_110;
                  position_5 = UnityEngine_Transform__get_position(&v115, axis, 0);
                  v89 = (__m128)*(unsigned __int64 *)&position_5->fields.x;
                  v90 = (__m128)LODWORD(position_5->fields.y);
                  v89.m128_f32[0] = v89.m128_f32[0] + 0.0;
                  v90.m128_f32[0] = v90.m128_f32[0] + 0.5;
                  if ( !component || !klass_2 )
                    goto LABEL_110;
                  ParticleManager__SetParticle(
                    klass_2,
                    11,
                    (UnityEngine_Vector2_o)*(_OWORD *)&_mm_unpacklo_ps(v89, v90),
                    HIDWORD(component[13].monitor),
                    0);
                }
              }
            }
            ++static_fields_3;
            ++m_Items;
            static_fields = (CreatePlant_o *)static_fields_3;
          }
        }
      }
      else
      {
        axis = this->fields.axis;
        if ( !axis )
          goto LABEL_110;
        position_6 = UnityEngine_Transform__get_position(&v115, axis, 0);
        v92 = (__m128)*(unsigned __int64 *)&position_6->fields.x;
        v93 = (__m128)LODWORD(position_6->fields.y);
        v92.m128_f32[0] = v92.m128_f32[0] + 0.0;
        v93.m128_f32[0] = v93.m128_f32[0] + 0.5;
        v95.fields.m_Mask = UnityEngine_LayerMask__op_Implicit(this->fields.zombieLayer.fields.m_Mask, 0).fields.m_Mask;
        if ( !UnityEngine_Physics2D_TypeInfo->_2.cctor_finished )
          il2cpp_runtime_class_init(UnityEngine_Physics2D_TypeInfo, v94);
        v96 = UnityEngine_Physics2D__OverlapCircleAll_6466051776(
                (UnityEngine_Vector2_o)*(_OWORD *)&_mm_unpacklo_ps(v92, v93),
                2.0,
                v95.fields.m_Mask,
                0);
        v97 = 0;
        if ( !v96 )
          goto LABEL_110;
        m_Items_1 = (CreatePlant_o **)v96->m_Items;
        while ( v97 < SLODWORD(v96->max_length) )
        {
          if ( v28 >= LODWORD(v96->max_length) )
LABEL_111:
            sub_180296EE0();
          static_fields = *m_Items_1;
          if ( !*m_Items_1 )
            goto LABEL_110;
          if ( UnityEngine_Component__TryGetComponent_object_(
                 (UnityEngine_Component_o *)static_fields,
                 &static_fields_4,
                 Method_UnityEngine_Component_TryGetComponent_DiamondRandomZombie___) )
          {
            static_fields = (CreatePlant_o *)static_fields_4;
            if ( !static_fields_4 )
              goto LABEL_110;
            if ( (int)sub_180344E80((unsigned int)(HIDWORD(static_fields_4[14].klass) - this->fields.thePlantRow), 0) <= 1 )
            {
              static_fields = (CreatePlant_o *)static_fields_4;
              if ( !static_fields_4 )
                goto LABEL_110;
              ((void (__fastcall *)(Il2CppObject *, __int64, const MethodInfo *))static_fields_4->klass->vtable[15].methodPtr)(
                static_fields_4,
                2,
                static_fields_4->klass->vtable[15].method);
            }
          }
          ++v28;
          ++m_Items_1;
          v97 = v28;
        }
      }
    }
    else
    {
      DiamondImitater__FireBall(this, 0);
    }
  }
  else
  {
    CreateZombie_TypeInfo_3 = CreateZombie_TypeInfo;
    if ( !CreateZombie_TypeInfo->_2.cctor_finished )
    {
      il2cpp_runtime_class_init(CreateZombie_TypeInfo, axis);
      CreateZombie_TypeInfo_3 = CreateZombie_TypeInfo;
    }
    axis = this->fields.axis;
    thePlantRow_5 = this->fields.thePlantRow;
    Instance_4 = CreateZombie_TypeInfo_3->static_fields->Instance;
    if ( !axis )
      goto LABEL_110;
    position_7 = UnityEngine_Transform__get_position(&v115, axis, 0);
    if ( !Instance_4 )
      goto LABEL_110;
    v107 = CreateZombie__SetZombie(Instance_4, thePlantRow_5, 215, position_7->fields.x, 0, 0);
    if ( !v107 )
      goto LABEL_110;
    static_fields_5 = UnityEngine_GameObject__GetComponent_object_(
                        v107,
                        Method_UnityEngine_GameObject_GetComponent_DiamondRandomZombie___);
    static_fields_4 = static_fields_5;
    if ( !static_fields_5 )
      goto LABEL_110;
    BYTE1(static_fields_5[39].monitor) = 1;
    if ( !static_fields_4 )
      goto LABEL_110;
    LODWORD(static_fields_4[9].klass) = 1;
    static_fields = (CreatePlant_o *)static_fields_4;
    if ( !static_fields_4 )
      goto LABEL_110;
    ((void (__fastcall *)(Il2CppObject *, _QWORD, __int64, _QWORD, const MethodInfo *))static_fields_4->klass->vtable[18].methodPtr)(
      static_fields_4,
      0,
      1,
      0,
      static_fields_4->klass->vtable[18].method);
  }
}

void DiamondImitater__FireBall(DiamondImitater_o *this, const MethodInfo *method)
{
  __m128 v2; // xmm0
  __int64 v4; // rdx
  __int64 v5; // rdx
  __int64 v6; // rdx
  __int64 v7; // rdx
  __int64 v8; // rdx
  __int64 v9; // rdx
  __int64 v10; // rdx
  __int64 v11; // rdx
  __int64 v12; // rdx
  Il2CppObject *object; // r15
  int32_t value_2; // eax
  __int64 v15; // r8
  struct Board_o *board; // rcx
  unsigned int theRow; // edi
  int32_t value; // r12d
  __int64 theRow_2; // rdx
  __int64 n32; // rsi
  struct Board_o *board_1; // rax
  struct BoxType_array *roadType; // rax
  Mouse_o *Instance; // rcx
  __m128 v24; // xmm6
  struct UnityEngine_Quaternion_StaticFields v25; // xmm7
  __int64 v26; // rdx
  UnityEngine_Transform_o *transform; // rbx
  Il2CppObject *v28; // rax
  Il2CppObject *Component_object; // rax
  Il2CppObject *Component_object_1; // rbx
  UnityEngine_GameObject_o *gameObject; // rax
  Il2CppObject *v32; // r14
  Il2CppObject *v33; // rax
  System_String_o *value_1; // rax
  UnityEngine_Vector3_o position_; // [rsp+30h] [rbp-88h] BYREF
  UnityEngine_Quaternion_o identityQuaternion; // [rsp+40h] [rbp-78h] BYREF
  unsigned int theRow_1; // [rsp+C0h] [rbp+8h] BYREF

  if ( !byte_1820C249A )
  {
    sub_180296CC0(&Method_UnityEngine_Component_GetComponent_SortingGroup___, method);
    sub_180296CC0(&Method_UnityEngine_GameObject_GetComponent_ZombieBall___, v4);
    sub_180296CC0(&int_TypeInfo, v5);
    sub_180296CC0(&Mouse_TypeInfo, v6);
    sub_180296CC0(&Method_UnityEngine_Object_Instantiate_GameObject____6475515200, v7);
    sub_180296CC0(&UnityEngine_Object_TypeInfo, v8);
    sub_180296CC0(&Method_UnityEngine_Resources_Load_GameObject___, v9);
    sub_180296CC0(&StringLiteral_6017, v10);
    sub_180296CC0(&StringLiteral_5432, v11);
    sub_180296CC0(&StringLiteral_10191, v12);
    byte_1820C249A = 1;
  }
  object = UnityEngine_Resources__Load_object_(StringLiteral_6017, Method_UnityEngine_Resources_Load_GameObject___);
  value_2 = UnityEngine_LayerMask__NameToLayer(StringLiteral_5432, 0);
  board = this->fields.board;
  theRow = 0;
  value = value_2;
  theRow_2 = 0;
  if ( !board )
LABEL_23:
    sub_180296EF0(board, theRow_2, v15);
  n32 = 32;
  while ( (int)theRow_2 < board->fields.rowNum )
  {
    board_1 = this->fields.board;
    if ( board_1 )
    {
      roadType = board_1->fields.roadType;
      if ( roadType )
      {
        if ( theRow >= LODWORD(roadType->max_length) )
          sub_180296EE0();
        if ( *(_DWORD *)((char *)&roadType->obj.klass + n32) != 1 )
        {
          Instance = Mouse_TypeInfo->static_fields->Instance;
          if ( !Instance )
            goto LABEL_24;
          v2.m128_f32[0] = Mouse__GetBoxXFromColumn(Instance, 0, 0);
          v24 = v2;
          Instance = Mouse_TypeInfo->static_fields->Instance;
          if ( !Instance )
            goto LABEL_24;
          v2.m128_f32[0] = Mouse__GetLandY(Instance, v2.m128_f32[0], theRow, 0);
          if ( !byte_1820C22B7 )
          {
            sub_180296CC0(&UnityEngine_Quaternion_TypeInfo, theRow_2);
            byte_1820C22B7 = 1;
          }
          v25 = *UnityEngine_Quaternion_TypeInfo->static_fields;
          Instance = (Mouse_o *)this->fields.board;
          if ( !Instance )
            goto LABEL_24;
          transform = UnityEngine_Component__get_transform((UnityEngine_Component_o *)Instance, 0);
          if ( !UnityEngine_Object_TypeInfo->_2.cctor_finished )
            il2cpp_runtime_class_init(UnityEngine_Object_TypeInfo, v26);
          v2 = _mm_unpacklo_ps(v24, v2);
          *(_QWORD *)&position_.fields.x = v2.m128_u64[0];
          position_.fields.z = 0.0;
          identityQuaternion = v25.identityQuaternion;
          v28 = UnityEngine_Object__Instantiate_object__6449599888(
                  object,
                  &position_,
                  &identityQuaternion,
                  transform,
                  (const MethodInfo_6D1590 *)Method_UnityEngine_Object_Instantiate_GameObject____6475515200);
          if ( !v28 )
            goto LABEL_24;
          Component_object = UnityEngine_GameObject__GetComponent_object_(
                               (UnityEngine_GameObject_o *)v28,
                               Method_UnityEngine_GameObject_GetComponent_ZombieBall___);
          Component_object_1 = Component_object;
          if ( !Component_object
            || (BYTE1(Component_object[5].monitor) = 1,
                (gameObject = UnityEngine_Component__get_gameObject((UnityEngine_Component_o *)Component_object, 0)) == 0)
            || (UnityEngine_GameObject__set_layer(gameObject, value, 0),
                v32 = UnityEngine_Component__GetComponent_object_(
                        (UnityEngine_Component_o *)Component_object_1,
                        Method_UnityEngine_Component_GetComponent_SortingGroup___),
                theRow_1 = theRow,
                v33 = (Il2CppObject *)il2cpp_value_box(int_TypeInfo, &theRow_1),
                value_1 = System_String__Format(StringLiteral_10191, v33, 0),
                !v32) )
          {
LABEL_24:
            sub_180296EF0(Instance, theRow_2, v15);
          }
          UnityEngine_Rendering_SortingGroup__set_sortingLayerName(
            (UnityEngine_Rendering_SortingGroup_o *)v32,
            value_1,
            0);
          HIDWORD(Component_object_1[2].klass) = theRow;
          HIDWORD(Component_object_1[5].klass) = 40;
        }
        board = this->fields.board;
        ++theRow;
        n32 += 4;
        theRow_2 = theRow;
        if ( board )
          continue;
      }
    }
    goto LABEL_23;
  }
}

// local variable allocation has failed, the output may be wrong!
bool DiamondImitater___c___AnimExplode_b__0_0(DiamondImitater___c_o *this, int32_t p, const MethodInfo *method)
{
  if ( !byte_1820C249C )
  {
    sub_180296CC0(&TypeMgr_TypeInfo, *(_QWORD *)&p);
    byte_1820C249C = 1;
  }
  if ( !TypeMgr_TypeInfo->_2.cctor_finished )
    il2cpp_runtime_class_init(TypeMgr_TypeInfo, *(_QWORD *)&p);
  return TypeMgr__IsWaterPlant(p, 0);
}

void DiamondImitater___c___cctor(const MethodInfo *method)
{
  __int64 v1; // rdx
  System_Configuration_ConfigurationCollectionAttribute_o *_; // rbx
  const MethodInfo *method_1; // r8

  if ( !byte_1820C249B )
  {
    sub_180296CC0(&DiamondImitater___c_TypeInfo, v1);
    byte_1820C249B = 1;
  }
  _ = (System_Configuration_ConfigurationCollectionAttribute_o *)sub_180245400(DiamondImitater___c_TypeInfo, v1);
  System_Configuration_ConfigurationCollectionAttribute___ctor(_, 0, method_1);
  DiamondImitater___c_TypeInfo->static_fields->__9 = (struct DiamondImitater___c_o *)_;
  sub_180296050(DiamondImitater___c_TypeInfo->static_fields, _);
}

void Imitater__AnimExplode(Imitater_o *this, const MethodInfo *method)
{
  __int64 thePlantRow_1; // r8
  Imitater_o *this_1; // rsi
  __int64 v4; // rdx
  __int64 v5; // rdx
  __int64 v6; // rdx
  __int64 v7; // rdx
  __int64 v8; // rdx
  __int64 v9; // rdx
  __int64 v10; // rdx
  UnityEngine_Transform_o *axis; // rdx
  UnityEngine_Vector3_o *position; // rax
  __m128 v13; // xmm0
  float z; // xmm2_4
  __m128 v15; // xmm1
  int32_t thePlantType; // r14d
  __int64 thePlantRow; // r9
  intptr_t m_CachedPtr; // rax
  __int64 v19; // rdx
  const MethodInfo *method_1; // r8
  System_Attribute_Fields v21; // rdi
  __int64 v22; // rdx
  __int64 v23; // rcx
  __int64 v24; // r8
  TypeMgr_c *TypeMgr_TypeInfo; // rax
  System_Collections_Generic_HashSet_uint__o *RedPlant; // rcx
  __int64 v27; // rdx
  __int64 v28; // rcx
  __int64 v29; // r8
  struct Board_o *board; // rax
  UnityEngine_Vector3_o *position_1; // rax
  __int64 v32; // rdx
  __m128 v33; // xmm0
  __m128 v34; // xmm7
  __m128 v35; // xmm6
  UnityEngine_Component_o *v36; // rax
  UnityEngine_GameObject_o *gameObject; // rbx
  struct Board_o *board_1; // rax
  UnityEngine_Camera_o *main; // rdi
  UnityEngine_Transform_o *transform; // rax
  UnityEngine_Vector3_o *position_2; // rax
  UnityEngine_Vector3_o *v42; // rax
  __int64 v43; // xmm1_8
  UnityEngine_Camera_o *main_1; // rax
  UnityEngine_Vector3_o *v45; // rax
  __m128 v46; // xmm6
  UnityEngine_Transform_o *transform_1; // rax
  __m128 v48; // xmm1
  UnityEngine_Vector3_o position_; // [rsp+40h] [rbp-98h] BYREF
  System_Configuration_ConfigurationCollectionAttribute_o v50; // [rsp+50h] [rbp-88h] BYREF
  System_Configuration_ConfigurationCollectionAttribute_o v51; // [rsp+68h] [rbp-70h] BYREF

  this_1 = this;
  if ( !byte_1820C250A )
  {
    sub_180296CC0(&Method_System_Collections_Generic_List_Enumerator_Plant__Dispose__, method);
    sub_180296CC0(&Method_System_Collections_Generic_List_Enumerator_Plant__MoveNext__, v4);
    sub_180296CC0(&Method_System_Collections_Generic_List_Enumerator_Plant__get_Current__, v5);
    sub_180296CC0(&Method_System_Collections_Generic_HashSet_PlantType__Contains__, v6);
    sub_180296CC0(&Lawnf_TypeInfo, v7);
    sub_180296CC0(&Method_System_Collections_Generic_List_Plant__GetEnumerator__, v8);
    sub_180296CC0(&UnityEngine_Object_TypeInfo, v9);
    sub_180296CC0(&TypeMgr_TypeInfo, v10);
    byte_1820C250A = 1;
  }
  axis = this_1->fields.axis;
  if ( !axis )
    goto LABEL_47;
  position = UnityEngine_Transform__get_position(&position_, axis, 0);
  v13 = (__m128)*(unsigned __int64 *)&position->fields.x;
  z = position->fields.z + 0.0;
  v15 = _mm_shuffle_ps(v13, v13, 85);
  v15.m128_f32[0] = v15.m128_f32[0] + 0.5;
  v13.m128_f32[0] = v13.m128_f32[0] + 0.0;
  *(_QWORD *)&position_.fields.x = _mm_unpacklo_ps(v13, v15).m128_u64[0];
  position_.fields.z = z;
  CreateParticle__SetParticle(11, &position_, this_1->fields.thePlantRow, 1, 0);
  ((void (__fastcall *)(Imitater_o *, __int64, const MethodInfo *))this_1->klass->vtable._20_Die.methodPtr)(
    this_1,
    2,
    this_1->klass->vtable._20_Die.method);
  thePlantType = this_1->fields.thePlantType;
  axis = (UnityEngine_Transform_o *)this_1->fields.board;
  if ( !axis )
    goto LABEL_47;
  axis = (UnityEngine_Transform_o *)axis[6].fields.m_CachedPtr;
  this = (Imitater_o *)this_1->fields.thePlantColumn;
  thePlantRow = this_1->fields.thePlantRow;
  if ( !axis )
    goto LABEL_47;
  m_CachedPtr = axis->fields.m_CachedPtr;
  if ( (unsigned int)this >= *(_DWORD *)m_CachedPtr
    || (thePlantRow_1 = *(_QWORD *)(m_CachedPtr + 16), (unsigned int)thePlantRow >= (unsigned int)thePlantRow_1) )
  {
    sub_180296EE0(this, axis, thePlantRow_1, thePlantRow);
  }
  this = (Imitater_o *)(thePlantRow + thePlantRow_1 * (_QWORD)this);
  axis = (UnityEngine_Transform_o *)*((_QWORD *)&axis[1].monitor + (_QWORD)this);
  if ( !axis )
    goto LABEL_47;
  axis = (UnityEngine_Transform_o *)axis->fields.m_CachedPtr;
  if ( !axis )
    goto LABEL_47;
  System_Collections_Generic_List_GameAPP_LastCards___GetEnumerator(
    (System_Collections_Generic_List_Enumerator_T__o *)&v50,
    (System_Collections_Generic_List_GameAPP_LastCards__o *)axis,
    (const MethodInfo_AB40C0 *)Method_System_Collections_Generic_List_Plant__GetEnumerator__);
  v51 = v50;
  v50.klass = 0;
  v50.monitor = &v51;
  while ( System_Collections_Generic_List_Enumerator_object___MoveNext(
            (System_Collections_Generic_List_Enumerator_T__o *)&v51,
            (const MethodInfo_8EE6D0 *)Method_System_Collections_Generic_List_Enumerator_Plant__MoveNext__) )
  {
    v21 = v51.fields.System_Attribute_Fields;
    if ( !UnityEngine_Object_TypeInfo->_2.cctor_finished )
      il2cpp_runtime_class_init(UnityEngine_Object_TypeInfo, v19);
    if ( UnityEngine_Object__op_Inequality(*(UnityEngine_Object_o **)&v21, 0, 0) )
    {
      if ( !*(_QWORD *)&v21 )
        sub_180296EF0(v23, v22, v24);
      if ( !*(_BYTE *)(*(_QWORD *)&v21 + 420LL)
        && !*(_BYTE *)(*(_QWORD *)&v21 + 423LL)
        && !*(_BYTE *)(*(_QWORD *)&v21 + 433LL)
        && !*(_BYTE *)(*(_QWORD *)&v21 + 438LL)
        && !*(_BYTE *)(*(_QWORD *)&v21 + 439LL) )
      {
        TypeMgr_TypeInfo = TypeMgr_TypeInfo;
        if ( !TypeMgr_TypeInfo->_2.cctor_finished )
        {
          il2cpp_runtime_class_init(TypeMgr_TypeInfo, v22);
          TypeMgr_TypeInfo = TypeMgr_TypeInfo;
        }
        RedPlant = (System_Collections_Generic_HashSet_uint__o *)TypeMgr_TypeInfo->static_fields->RedPlant;
        if ( !RedPlant )
          sub_180296EF0(0, v22, v24);
        if ( !System_Collections_Generic_HashSet_uint___Contains(
                RedPlant,
                *(_DWORD *)(*(_QWORD *)&v21 + 348LL),
                Method_System_Collections_Generic_HashSet_PlantType__Contains__)
          && *(_DWORD *)(*(_QWORD *)&v21 + 348LL) != 245 )
        {
          if ( *(_DWORD *)(*(_QWORD *)&v21 + 348LL) != 1151 )
            goto LABEL_29;
          board = this_1->fields.board;
          if ( !board )
            sub_180296EF0(v28, v27, v29);
          if ( !board->fields.boardTag.fields.isRogue )
LABEL_29:
            thePlantType = *(_DWORD *)(*(_QWORD *)&v21 + 348LL);
        }
      }
    }
  }
  System_Configuration_ConfigurationCollectionAttribute___ctor(
    &v51,
    Method_System_Collections_Generic_List_Enumerator_Plant__Dispose__,
    method_1);
  axis = this_1->fields.axis;
  if ( !axis )
    goto LABEL_47;
  position_1 = UnityEngine_Transform__get_position(&position_, axis, 0);
  v33 = (__m128)*(unsigned __int64 *)&position_1->fields.x;
  position_.fields.z = position_1->fields.z;
  v34 = v33;
  v34.m128_f32[0] = v33.m128_f32[0] + 0.0;
  v35 = _mm_shuffle_ps(v33, v33, 85);
  *(_QWORD *)&position_.fields.x = v33.m128_u64[0];
  v35.m128_f32[0] = v35.m128_f32[0] + 0.5;
  if ( !Lawnf_TypeInfo->_2.cctor_finished )
    il2cpp_runtime_class_init(Lawnf_TypeInfo, v32);
  v36 = (UnityEngine_Component_o *)Lawnf__SetDroppedCard(
                                     (UnityEngine_Vector2_o)*(_OWORD *)&_mm_unpacklo_ps(v34, v35),
                                     thePlantType,
                                     0,
                                     0);
  if ( !v36 )
    goto LABEL_47;
  gameObject = UnityEngine_Component__get_gameObject(v36, 0);
  board_1 = this_1->fields.board;
  if ( !board_1 )
    goto LABEL_47;
  if ( !board_1->fields.boardTag.fields.isBigMap )
    return;
  main = UnityEngine_Camera__get_main(0);
  if ( !gameObject )
    goto LABEL_47;
  transform = UnityEngine_GameObject__get_transform(gameObject, 0);
  if ( !transform )
    goto LABEL_47;
  position_2 = UnityEngine_Transform__get_position((UnityEngine_Vector3_o *)&v50, transform, 0);
  if ( !main )
    goto LABEL_47;
  position_ = *position_2;
  v42 = UnityEngine_Camera__WorldToViewportPoint_6465490576((UnityEngine_Vector3_o *)&v50, main, &position_, 0);
  v43 = *(_QWORD *)&v42->fields.x;
  position_.fields.z = v42->fields.z;
  *(_QWORD *)&position_.fields.x = v43;
  if ( *(float *)&v43 <= 0.0 || *(float *)&v43 >= 1.0 || position_.fields.y <= 0.0 || position_.fields.y >= 1.0 )
  {
    main_1 = UnityEngine_Camera__get_main(0);
    if ( main_1 )
    {
      *(_QWORD *)&position_.fields.x = _mm_unpacklo_ps((__m128)0x3F000000u, (__m128)0x3F000000u).m128_u64[0];
      position_.fields.z = 0.0;
      v45 = UnityEngine_Camera__ViewportToWorldPoint_6465489840((UnityEngine_Vector3_o *)&v50, main_1, &position_, 0);
      v46 = (__m128)*(unsigned __int64 *)&v45->fields.x;
      position_.fields.z = v45->fields.z;
      transform_1 = UnityEngine_GameObject__get_transform(gameObject, 0);
      v48 = _mm_shuffle_ps(v46, v46, 85);
      *(_QWORD *)&position_.fields.x = v46.m128_u64[0];
      if ( transform_1 )
      {
        *(_QWORD *)&position_.fields.x = _mm_unpacklo_ps(v46, v48).m128_u64[0];
        position_.fields.z = 0.0;
        UnityEngine_Transform__set_position(transform_1, &position_, 0);
        return;
      }
    }
LABEL_47:
    sub_180296EF0(this, axis, thePlantRow_1);
  }
}

void Imitater__AttributeEvent(Imitater_o *this, const MethodInfo *method)
{
  const MethodInfo *method_1; // r8
  __int64 v4; // rdx
  __int64 v5; // r8
  UnityEngine_Animator_o *anim; // rcx

  if ( !byte_1820C250B )
  {
    sub_180296CC0(&StringLiteral_950, method);
    byte_1820C250B = 1;
  }
  System_Configuration_ConfigurationCollectionAttribute___ctor(
    (System_Configuration_ConfigurationCollectionAttribute_o *)this,
    0,
    method_1);
  anim = this->fields.anim;
  if ( !anim )
    sub_180296EF0(0, v4, v5);
  UnityEngine_Animator__SetTriggerString(anim, StringLiteral_950, 0);
}