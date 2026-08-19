<!--
{% comment %}
Copyright (c) Microsoft Corporation.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
{% endcomment %}
-->

# substrait-csharp

Experimental C# bindings for [substrait](https://substrait.io).

## Development

The repository requires a .NET 8 SDK. The checked-in `global.json` selects a
compatible installed SDK.

Clone the repository with its pinned Substrait specification sources:

```shell
git clone --recurse-submodules https://github.com/microsoft/substrait-csharp.git
```

For an existing clone, initialize the submodule with:

```shell
git submodule update --init --recursive
```

Restore, build, and test the solution from the repository root:

```shell
dotnet restore Substrait.sln
dotnet build Substrait.sln --configuration Release --no-restore
dotnet test Substrait.sln --configuration Release --no-build
```

Create the NuGet package locally with:

```shell
dotnet pack src/Substrait/Substrait.csproj --configuration Release --output artifacts/packages
```

The package and assembly identity are provisional until the first package
preview. See [the development plan](docs/preview-package.md) for the staged contribution
work and open decisions.

The `third_party/substrait` submodule pins the upstream specification used to
generate protobuf and type-parser code. To upgrade it, check out the desired
upstream release in that directory and commit the updated submodule pointer.

## Contributing

Here are some ways you can contribute to the substrait-csharp project:

* Submit PRs to fix bugs or add new features.
* Review currently [open PRs](https://github.com/microsoft/substrait-csharp/pulls).
* Provide feedback and report bugs related to the software or the documentation.
* Enhance our design documents, examples, tutorials, and overall documentation.

To get started, please take a look at the [issues](https://github.com/microsoft/substrait-csharp/issues) and leave a comment if any of them interest you.

If you plan to make significant changes, we recommend [discussing](https://github.com/microsoft/substrait-csharp/discussions) them with the substrait-csharp community first.
This helps ensure that your contributions align with the project's goals and avoids duplicating efforts.

## Contributor License Agreement

Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## License

See the [LICENSE](LICENSE) file for more details.
