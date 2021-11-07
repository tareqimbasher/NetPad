import {IHttpClient} from "aurelia";
import * as monaco from "monaco-editor";
import {Query, Settings} from "@domain";
import {Mapper} from "@common";

export class Index {
  public queries: Query[] = [];
  
  constructor(@IHttpClient readonly httpClient: IHttpClient, private settings: Settings) {
  }
  
  public async attached() {
    const response = await this.httpClient.get("queries");
    this.queries = (await response.json() as string[]).map(name => {return {name: name}}).map(o => Mapper.toModel(Query, o));
    console.log(this.queries);
    
    const el = document.querySelector('.text-editor') as HTMLElement;
    const editor = monaco.editor.create(el, {
      value: 'Console.WriteLine("Hello World");',
      language: 'csharp',
      theme: "vs-dark"
    });
    
    window.addEventListener("resize", () => editor.layout());
    // const ob = new ResizeObserver(entries => editor.layout());
    // ob.observe(document.querySelector(".content"));
  }
}

class Proxy
{
  // Get open queries
  // Create new
  // Save
  // Open existing
  
  // Update code
  // Rename query
  // Run Query
  // Reference DLLs and Packages
  
  // Autocomplete
  
}
