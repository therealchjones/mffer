using System;
using System.Dynamic;

namespace Mffer {
	/// <summary>
	/// <see cref="GetMemberBinder"/> derivative used to access members of <see
	/// cref="AssetsTools.Dynamic.DynamicAsset"/>s
	/// </summary>
	internal class AssetsToolsMemberBinder : GetMemberBinder {
		/// <summary>
		/// Initializes a new <see cref="AssetsToolsMemberBinder"/> instance
		/// </summary>
		/// <param name="name">Name of the member to access</param>
		/// <param name="ignoreCase"><c>true</c> if the access should not be
		/// case-sensitive, <c>false</c> otherwise</param>
		public AssetsToolsMemberBinder( string name, bool ignoreCase ) : base( name, ignoreCase ) { }
		/// <summary>
		/// Non-implemented method to allow derivation from abstract class
		/// </summary>
		/// <remarks>Do not use.</remarks>
		/// <param name="target">Not applicable. Do not use.</param>
		/// <param name="errorSuggestion">Not applicable. Do not use.</param>
		/// <returns>Not applicable. Do not use.</returns>
		public override DynamicMetaObject FallbackGetMember( DynamicMetaObject target, DynamicMetaObject errorSuggestion ) {
			throw new NotImplementedException();
		}
	}
}
