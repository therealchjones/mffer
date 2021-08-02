class Deployment {
	public hostUri: string;
	public appsScriptUri: string;
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
