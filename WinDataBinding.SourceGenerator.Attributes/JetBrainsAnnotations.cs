// Annotations for JetBrains tools like ReSharper and Rider.
// Only the annotations needed for this project are included.

using System;

// ReSharper disable once CheckNamespace
namespace JetBrains.Annotations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.GenericParameter)]
    internal sealed class MeansImplicitUseAttribute : Attribute
    {
        public MeansImplicitUseAttribute()
            : this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default) { }

        public MeansImplicitUseAttribute(ImplicitUseKindFlags useKindFlags)
            : this(useKindFlags, ImplicitUseTargetFlags.Default) { }

        public MeansImplicitUseAttribute(ImplicitUseTargetFlags targetFlags)
            : this(ImplicitUseKindFlags.Default, targetFlags) { }

        public MeansImplicitUseAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags)
        {
            UseKindFlags = useKindFlags;
            TargetFlags = targetFlags;
        }

        public ImplicitUseKindFlags UseKindFlags { get; }

        public ImplicitUseTargetFlags TargetFlags { get; }
    }

    [Flags]
    internal enum ImplicitUseKindFlags
    {
        Default = Access | Assign | InstantiatedWithFixedConstructorSignature,

        Access = 1,

        Assign = 2,

        InstantiatedWithFixedConstructorSignature = 4,

        InstantiatedNoFixedConstructorSignature = 8,
    }

    [Flags]
    internal enum ImplicitUseTargetFlags
    {
        Default = Itself,

        Itself = 1,

        Members = 2,

        WithInheritors = 4,

        WithMembers = Itself | Members,
    }
}

