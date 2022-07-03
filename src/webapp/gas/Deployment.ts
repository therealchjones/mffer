class Deployment {
	public hostUri: string | null = null;
	public appsScriptUri: string | null = null;
	constructor() {}

	public static getCurrentDeployment(): Deployment {
		let deployment = new Deployment();
		let scriptProperties = PropertiesService.getScriptProperties();

		return deployment;
	}
}
class DeploymentProperties {
	constructor() {}
}
