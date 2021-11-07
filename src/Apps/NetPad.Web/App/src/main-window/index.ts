import {IHttpClient} from "aurelia";
import * as monaco from "monaco-editor";

export class Index {
  public queries: string[] = [];
  
  constructor(@IHttpClient readonly httpClient: IHttpClient) {
  }
  
  public async attached() {
    const response = await this.httpClient.get("queries");
    console.log(response.ok, await response.json());
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
