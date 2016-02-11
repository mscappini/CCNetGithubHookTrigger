# CruiseControl.NET Github Hook Trigger
A CCNet trigger plugin created to respond to Github's web hooks. (Currently only `ping` and `push`.)

## Defaults

- Binds to all addresses (`*`)
- Listens on port `31574` 
- Does not log any information
- `master` branch triggers build
- Build condition is `IfModificationExists`

## ccnet.config

### Trigger name
`githubHookTrigger`

### Options
- `endpoint`: A valid listening endpoint (e.g. `http://*:31574`).
- `logfile`: A path to a logfile (enables logging). (e.g. `C:\CCNet\logs\githubHookTrigger.log`).
- `secret`: A secret configured exactly as in Github.
- `branches`: An array of branches to trigger the build. Supports regular expression matching.
- `buildCondition`: The build condition to return to the integrator. (See [build conditions](http://cruisecontrolnet.org/projects/ccnet/wiki/Build_Condition).)

## Examples
```
<triggers>
  <githubHookTrigger />
</triggers>
```

```
<triggers>
  <githubHookTrigger endpoint="http://58.96.46.38:5432"
                     logfile="C:\CCNet\Logs\githubHookTriggerLog.txt"
                     secret="foobar"
                     buildCondition="ForceBuild">
    <branches>
      <string>master</string>
      <string>develop</string>
      <string>(?&lt;version&gt;((\d+)((\.\d+)?){1,}))</string>
    </branches>
  </githubHookTrigger>
</triggers>
```

## Integration
The Github Hook Trigger provides a dynamic value named `Branch` that is the name of the branch provided in the `ref` element of the push notification. This value can be used as a dynamic value later in the build process.

## Caveats
- As far as I know, this plugin will not load in a stable release of CCNet <= **1.8.5.0** (the latest stable release at the time of writing). I tested this plugin with CCNet nightly build **1.9.70.0**. The stable releases were built with .NET 2.0 and cannot load a .NET 4.5 plugin (the version this plugin was built with). The nightlies are built with .NET 4.5.
- This project depends on OWIN. There exists a bug where one of the OWIN packages is built with a version that another dependency does not like. In order to fix this, I had to apply this configuration change to `ccservice.exe.config` in the CCNet installation directory:

```
<configuration>
	<runtime>
		<assemblyBinding>
			<dependentAssembly>
				<assemblyIdentity name="Microsoft.Owin" publicKeyToken="31bf3856ad364e35" culture="neutral" />
				<bindingRedirect oldVersion="0.0.0.0-3.0.1.0" newVersion="3.0.1.0" />
			</dependentAssembly>
			<dependentAssembly>
				<assemblyIdentity name="System.Web.Http" publicKeyToken="31bf3856ad364e35" culture="neutral" />
				<bindingRedirect oldVersion="0.0.0.0-5.2.3.0" newVersion="5.2.3.0" />
			</dependentAssembly>
		</assemblyBinding>
	</runtime>
</configuration>
```

## SSL
To configure for SSL, you must use the `netsh` utility (`netsh` is Win7+, previous versions used `httpcfg`.) Explicit configuration for SSL is beyond the scope of this document, but I will include a quick line and example for reference.

Reference:

	netsh http add sslcert ipport=0.0.0.0:31574 certhash=<selected SSL thumbprint> appid={<guid>}

Example:

	netsh http add sslcert ipport=0.0.0.0:31574 certhash=1a5d02a4ea3623a99defe789fe3fa35fa9af5523b appid={df97eab5-1aba-4069-b364-825799f129a0}

Configure `githubHookTrigger` `endpoint` attribute to be `https://+:31574/`.

# License

GithubHookTrigger is under the MIT license.