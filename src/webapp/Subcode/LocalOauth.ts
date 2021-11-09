class LocalOauth {
	constructor(
		serviceName: string,
		properties: { [key: string]: string } = null
	) {}
	private storage_: VolatileProperties = null;
}
