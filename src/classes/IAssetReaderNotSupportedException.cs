using System;

namespace Mffer {
	public class IAssetReaderNotSupportedException : NotSupportedException {
		public IAssetReaderNotSupportedException() :
			base( "Static asset-related methods are not available; instantiate an implementor of the IAssetReader interface instead." ) {

		}
	}
}
