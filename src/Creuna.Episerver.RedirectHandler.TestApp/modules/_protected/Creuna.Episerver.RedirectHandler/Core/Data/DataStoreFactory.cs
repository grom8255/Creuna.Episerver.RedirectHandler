//using System;
//using EPiServer.Data.Dynamic;
//using EPiServer.ServiceLocation;

//namespace Creuna.Episerver.RedirectHandler.Core.Data
//{
//    public interface IRedirectStoreFactory
//    {
//        IRedirectStore GetStore();
//    }

//    [ServiceConfiguration(typeof(IRedirectStoreFactory), Lifecycle = ServiceInstanceScope.Singleton)]
//    public class DdsRedirectStoreFactory : IRedirectStoreFactory
//    {
//        public virtual DdsRedirectStore GetStore()
//        {
//            // GetStore will only return null the first time this method is called for a Type
//            // In that case the ?? C# operator will call CreateStore
//            // EPiServer.Data.Dynamic.DynamicDataStoreFactory.Instance.DeleteStore(t,true);
//            return new DdsRedirectStore(DynamicDataStoreFactory.Instance.GetStore(t) ??
//                   DynamicDataStoreFactory.Instance.CreateStore(t));
//        }

//        IRedirectStore IRedirectStoreFactory.GetStore()
//        {
//            return GetStore();
//        }
//    }
//}