using System;

namespace FFS.Libraries.StaticEcs {
// required for codegen
    
// Component types
public class CmpAttribute : Attribute { }
public class MulAttribute : Attribute { }
public class EvtAttribute : Attribute { }

public class FeatureAttribute : Attribute { }

// System types
public class PreAttribute : Attribute { }
public class UpdAttribute : Attribute { }
public class LatAttribute : Attribute { }
public class FixAttribute : Attribute { }
public class DrwAttribute : Attribute { }
public class ClnAttribute : Attribute { }

}