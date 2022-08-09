class VolatileProperties {
	constructor(properties: { [key: string]: string | null } | null = null) {
		if (properties == null) properties = {};
		this.properties_ = {};
		for (const key of Object.keys(properties)) {
			let value = properties[key];
			if (!value) this.properties_[key] = "";
			else this.properties_[key] = value.toString();
		}
	}
	private properties_: { [key: string]: string };
	public deleteAllProperties(): VolatileProperties {
		this.properties_ = {};
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
		if (this.getKeys().includes(key)) return this.properties_[key];
		else return null;
	}
	public setProperties(
		properties: { [key: string]: string },
		deleteAllOthers: boolean = false
	): VolatileProperties {
		if (properties == null) properties = {};
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
	public setProperty(key: string, value: string | null): VolatileProperties {
		if (!value) value = "";
		this.properties_[key] = value;
		return this;
	}
}
