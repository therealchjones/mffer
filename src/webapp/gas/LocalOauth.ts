class LocalOauth {
	constructor(
		serviceName: string,
		properties: { [key: string]: string } | null = null
	) {}
	private storage_: VolatileProperties | null = null;
}
