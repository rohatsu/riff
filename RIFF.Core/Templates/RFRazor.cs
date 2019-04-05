// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RazorEngine.Compilation;
using RazorEngine.Templating;
using RazorEngine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
#if !NETSTANDARD2_0
using System.Web.Razor.Parser;
#endif

namespace RIFF.Core
{
#if NETSTANDARD2_0
    internal class ParserHelpers
    {
        public static string SanitizeClassName(string className)
        {
            var sb = new StringBuilder();
            foreach (var c in className)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }
            return sb.ToString();
        }
    }
#endif

    public static class RFRazor
    {
        private static ICachingProvider _cachingProvider;
        private static IRazorEngineService _razorService;
        private static volatile object _sync = new object();

        public static void Initialize(IEnumerable<string> assemblyNames = null)
        {
            // do not reinit
            if (_razorService == null)
            {
                _cachingProvider = new PrecompiledCachingProvider();

                var config = new RazorEngine.Configuration.TemplateServiceConfiguration
                {
                    DisableTempFileLocking = true,
                    EncodedStringFactory = new RawStringFactory(),
                    //TemplateManager = new WatchingResolvePathTemplateManager(templatePaths, new InvalidatingCachingProvider()),
                    TemplateManager = new PrecompiledTemplateManager(),
                    CachingProvider = _cachingProvider
                };

                _razorService = RazorEngineService.Create(config);

                // load templates
                var assemblies = new SortedSet<string> { "RIFF.Core", "RIFF.Framework" };
                if (assemblyNames != null)
                {
                    foreach (var assembly in assemblyNames)
                    {
                        if (assembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        {
                            assemblies.Add(assembly.Substring(0, assembly.Length - 4));
                        }
                        else
                        {
                            assemblies.Add(assembly);
                        }
                    }
                }
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => assemblies.Contains(a.GetName().Name)))
                {
                    CacheTemplates(assembly);
                }
            }
        }

        public static string RunTemplate(Type templateType, object model)
        {
            if (_razorService == null)
            {
                throw new RFSystemException(typeof(RFRazor), "Razor templates not initialized.");
            }
            lock (_sync)
            {
                var templateName = ParserHelpers.SanitizeClassName(templateType.FullName).ToLowerInvariant();
                return _razorService.Run(templateName, null, model);
            }
        }

        private static void CacheTemplate(string templateName, Type templateType, bool isLayout)
        {
            var key = new PrecompiledTemplateKey(templateName, isLayout ? ResolveType.Layout : ResolveType.Global);
            _cachingProvider.CacheTemplate(new PrecompiledTemplate(key, templateType.Assembly, templateType), key);
        }

        private static void CacheTemplates(Assembly assembly)
        {
            int n = 0;
            try
            {
                var templateType = typeof(ITemplate);
                var views = assembly.GetExportedTypes().Where(x => templateType.IsAssignableFrom(x));

                foreach (var view in views)
                {
                    CacheTemplate(view.FullName, view, view.Name.StartsWith("_", StringComparison.Ordinal));
                    n++;
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Exception(typeof(RFRazor), ex, "Error caching templates from assembly {0}", assembly.FullName);
            }
        }
    }

    public class PrecompiledCachingProvider : ICachingProvider
    {
        public TypeLoader TypeLoader
        {
            get
            {
                return _proxy.TypeLoader;
            }
        }

        protected DefaultCachingProvider _proxy;

        public PrecompiledCachingProvider()
        {
            _proxy = new DefaultCachingProvider(t => { });
        }

        public void CacheTemplate(ICompiledTemplate template, ITemplateKey key)
        {
            _proxy.CacheTemplate(template, key);
        }

        public bool TryRetrieveTemplate(ITemplateKey key, Type modelType, out ICompiledTemplate template)
        {
            return _proxy.TryRetrieveTemplate(key, null, out template);
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above. GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (_proxy != null)
                    {
                        _proxy.Dispose();
                        _proxy = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free
        //       unmanaged resources. ~PrecompiledCachingProvider() { // Do not change this code. Put
        // cleanup code in Dispose(bool disposing) above. Dispose(false); }

        #endregion IDisposable Support
    }

    public class PrecompiledTemplate : ICompiledTemplate
    {
        public CompilationData CompilationData
        {
            get
            {
                return new CompilationData(null, null);
            }
        }

        public ITemplateKey Key
        {
            get
            {
                return _templateKey;
            }
        }

        public Type ModelType
        {
            get
            {
                return null;
            }
        }

        public ITemplateSource Template
        {
            get
            {
                return new LoadedTemplateSource(String.Empty);
            }
        }

        public Assembly TemplateAssembly
        {
            get
            {
                return _templateAssembly;
            }
        }

        public Type TemplateType
        {
            get
            {
                return _templateType;
            }
        }

        protected Assembly _templateAssembly;
        protected ITemplateKey _templateKey;
        protected Type _templateType;

        public PrecompiledTemplate(ITemplateKey templateKey, Assembly templateAssembly, Type templateType)
        {
            _templateKey = templateKey;
            _templateAssembly = templateAssembly;
            _templateType = templateType;
        }
    }

    public class PrecompiledTemplateKey : BaseTemplateKey
    {
        public PrecompiledTemplateKey(string name, ResolveType resolveType) : base(ParserHelpers.SanitizeClassName(name).ToLowerInvariant(), resolveType, null)
        {
        }

        public override string GetUniqueKeyString()
        {
            return this.Name;
        }
    }

    public class PrecompiledTemplateManager : ITemplateManager
    {
        public void AddDynamic(ITemplateKey key, ITemplateSource source)
        {
        }

        public ITemplateKey GetKey(string name, ResolveType resolveType, ITemplateKey context)
        {
            return new PrecompiledTemplateKey(name, resolveType);
        }

        public ITemplateSource Resolve(ITemplateKey key)
        {
            return new LoadedTemplateSource(null, key.Name);
        }
    }
}
