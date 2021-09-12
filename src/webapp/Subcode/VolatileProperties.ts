class VolatileProperties {
	constructor(properties: { [key: string]: string } = null) {
		this.properties_ = properties;
	}
	private properties_: { [key: string]: string };
	public deleteAllProperties(): VolatileProperties {
		this.properties_ = null;
		return this;
	}
	public deleteProperty(key: string): VolatileProperties {
		delete this.properties_[key];
		return this;
	}
	public getKeys(): string[] {
		return Object.keys(this.properties_);
	}
	public getProperties(): { [key: string]: string } {
		return this.properties_;
	}
	public getProperty(key: string) {
		return this.properties_[key];
	}
	public setProperties(
		properties: { [key: string]: string },
		deleteAllOthers: boolean = false
	): VolatileProperties {
		if (deleteAllOthers === true) {
			this.properties_ = properties;
		} else {
			let newProperties = {
				...this.properties_,
				...properties,
			};
			this.properties_ = newProperties;
		}
		return this;
	}
	public setProperty(key: string, value: string): VolatileProperties {
		this.properties_[key] = value;
		return this;
	}
}
