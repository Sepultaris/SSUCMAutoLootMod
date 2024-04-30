using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace SSUCM_AutoLootMod
{
    enum ComparisonType
    {
        Equal,
        NotEqual,
        LessThanOrEqual,
        GreaterThanOrEqual,
    }

    internal class AutoLootRequirement
    {
        public string Value { get; set; }
        public ComparisonType Comparison { get; set; }
        public string PropertyName { get; set; }

    }
}
