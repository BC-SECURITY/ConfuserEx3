using System;
using System.IO;
using System.Reflection;
using dnlib.DotNet;

namespace Confuser.Core.Services {
	internal class RuntimeService : IRuntimeService {
		private readonly ConfuserContext context;
		ModuleDef rtModule;

		public RuntimeService(ConfuserContext context) {
			this.context = context;
		}

		/// <inheritdoc />
		public TypeDef GetRuntimeType(string fullName) {
			if (rtModule == null) {
				LoadConfuserRuntimeModule();
			}
			return rtModule.Find(fullName, true);
		}

		private void LoadConfuserRuntimeModule() {
			var entryPoint = this.context.Project[0].Resolve(this.context.Project.BaseDirectory);
			var hasMscorlib = entryPoint.GetAssemblyRef("mscorlib") != null;
			// Build the path with Path.Combine so the separator matches the OS. A literal
			// with '\\' only resolves on Windows; on Linux/macOS the backslash is not a
			// directory separator, so the runtime module is not found and obfuscation
			// fails with an IOException on the first phase that injects it.
			var runtimeDllName = hasMscorlib
				? Path.Combine("runtime", "net472", "Confuser.Runtime.dll")
				: Path.Combine("runtime", "net8.0", "Confuser.Runtime.dll");

			var module = typeof(RuntimeService).Assembly.ManifestModule;
			string rtPath = runtimeDllName;
			var creationOptions = new ModuleCreationOptions() { TryToLoadPdbFromDisk = true };
			if (module.FullyQualifiedName[0] != '<') {
				rtPath = Path.Combine(Path.GetDirectoryName(module.FullyQualifiedName), rtPath);
				if (File.Exists(rtPath)) {
					try {
						rtModule = ModuleDefMD.Load(rtPath, creationOptions);
					}
					catch (IOException) { }
				}
				if (rtModule == null) {
					rtPath = runtimeDllName;
				}
			}
			if (rtModule == null) {
				rtModule = ModuleDefMD.Load(rtPath, creationOptions);
			}
			rtModule.EnableTypeDefFindCache = true;
		}
	}

	/// <summary>
	///     Provides methods to obtain runtime library injection type.
	/// </summary>
	public interface IRuntimeService {
		/// <summary>
		///     Gets the specified runtime type for injection.
		/// </summary>
		/// <param name="fullName">The full name of the runtime type.</param>
		/// <returns>The requested runtime type.</returns>
		TypeDef GetRuntimeType(string fullName);
	}
}
