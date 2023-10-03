namespace NeosModConfig
{
	/// <summary>
	/// Defines options for the handling of incompatible configuration versions.
	/// </summary>
	public enum IncompatibleConfigurationHandlingOption
	{
		/// <summary>
		/// Fail to read the config, and block saving over the config on disk.
		/// </summary>
		ERROR,

		/// <summary>
		/// Destroy the saved config and start over from scratch.
		/// </summary>
		CLOBBER,

		/// <summary>
		/// Ignore the version number and attempt to load the config from disk.
		/// </summary>
		FORCE_LOAD,
	}
}
