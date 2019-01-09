using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf.Sdk.CSharp.State
{
    public class StructuredState : StateBase
    {
        internal Dictionary<string, PropertyInfo> PropertyInfos;

        public StructuredState()
        {
            DetectPropertyInfos();
            InitializeProperties();
        }

        private void DetectPropertyInfos()
        {
            PropertyInfos = this.GetType().GetProperties().Where(x => x.PropertyType.IsSubclassOf(typeof(StateBase)))
                .ToDictionary(x => x.Name, x => x);
        }

        private void InitializeProperties()
        {
            foreach (var kv in PropertyInfos)
            {
                var propertyInfo = kv.Value;
                var type = propertyInfo.PropertyType;
                var instance = Activator.CreateInstance(type);
                propertyInfo.SetValue(this, instance);
            }
        }

        internal override void OnPathSet()
        {
            foreach (var kv in PropertyInfos)
            {
                var propertyInfo = kv.Value;
                var path = this.Path.Clone();
                path.Path.Add(ByteString.CopyFromUtf8(kv.Key));
                ((StateBase) propertyInfo.GetValue(this)).Path = path;
            }

            base.OnPathSet();
        }

        internal override void OnStateManagerSet()
        {
            foreach (var kv in PropertyInfos)
            {
                var propertyInfo = kv.Value;
                ((StateBase) propertyInfo.GetValue(this)).Manager = this.Manager;
            }

            base.OnStateManagerSet();
        }

        internal override void Clear()
        {
            foreach (var kv in PropertyInfos)
            {
                var propertyInfo = kv.Value;
                ((StateBase) propertyInfo.GetValue(this)).Clear();
            }
        }

        internal override Dictionary<StatePath, StateValue> GetChanges()
        {
            var dict = new Dictionary<StatePath, StateValue>();
            foreach (var kv in PropertyInfos)
            {
                var propertyInfo = kv.Value;
                var propertyValue = (StateBase) propertyInfo.GetValue(this);
                foreach (var kv1 in propertyValue.GetChanges())
                {
                    dict[kv1.Key] = kv1.Value;
                }
            }

            return dict;
        }
    }
}