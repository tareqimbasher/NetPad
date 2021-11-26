# SPA App

## Top Level Folders

* `core`: folders contained here should be thought of as separate modules.
    * `@common`: contains common utils and non-app specific code.
    * `@domain`: the app-specific domain.
* `styles`: application-wide styling.
* `windows`: each window should have a separate folder within this folder.

### Note

> When adding a folder to `core`, be sure to add it to `tsconfig.json`
> as well so that it can be referenced as a module, without relative
> paths, ex:
> ```typescript
> import {something} from "@domain";
> ```

## REST API

### Swagger

To remove the need to constantly create and maintain JS code to interact with
the REST API, Swagger is being used to auto-generate the file `@domain/api.ts`
whenever the API interface changes. Swagger generates API client classes and
interfaces for each REST API controller in the ASP.NET Core app with methods for
each endpoint, as well as models representing all the data types the API
interface exposes.

Loading the page `http://API_URL/swagger/index.html` will regenerate the file.

Swagger will generate an API client class and an interface for every API
controller it finds. The class implementation includes methods for each endpoint
declared by the controller.

For an API controller named `ScriptsController` that has a `GET` controller
action named `GetScripts`, Swagger will generate a `ScriptsApiClient` class and
a `IScriptsApiClient` interface.

Example:

```typescript
export interface IScriptsApiClient {
    getScripts(): Promise<Script[]>;
}

export class ScriptsApiClient implements IScriptsApiClient {
    getScripts(): Promise<Script[]> {
        //...
    }
}
```

### Using `api.ts`

We want to create our own services that extend the auto-generated api clients
that Swagger generated, instead of using the auto-generated clients directly.
This is how you would go about doing that.

After creating a new API controller, load the swagger page so it regenerates
the `api.ts` file. For the sake of this example, let say you created a
`PersonsController`. The Swagger tooling will generate a `PersonsApiClient`
class and a `IPersonsApiClient` interface.

1. In `@domain`, create a `IPersonService` interface that _extends_ the
   auto-generated `IPersonsApiClient` interface.
2. In `@domain`, create a `PersonService` class that _extends_ the
   auto-generated `PersonsApiClient` class and _implements_ the
   `IPersonService` interface you created.
3. Create an interface Symbol using Aurelia's `DI.createInterface<>()`
   method that can be used in registering the new `PersonService` with the DI
   container.
4. Register your new `PersonService` with the DI container.

**Full example:**
```typescript
import {DI} from "aurelia";
import {IPersonsApiClient, PersonsApiClient} from "@domain";

export interface IPersonService extends IPersonsApiClient {
}

export const IPersonService = DI.createInterface<IPersonService>();

export class PersonService extends PersonsApiClient implements IPersonService {
    // You can add more methods here or override methods from the 
    // PersonsApiClient implementation
}
```

Register your implementation with DI container in `main.ts`:
```typescript
app.register(Registration.singleton(IPersonService, PersonService));
```

Now you can use it like this:

```typescript
export class SomeComponent {
    constructor(@IPersonService readonly personService: IPersonService) {
    }
    
    public async load() {
        const persons = await this.personService.get();
    }
}
```