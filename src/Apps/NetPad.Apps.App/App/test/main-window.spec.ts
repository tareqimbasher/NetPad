import {render} from './helper';
import {Index} from '../src/windows/main';

describe('my-app', () => {
  it('should render message', async () => {
    const node = (await render('<my-app></my-app>', Index)).firstElementChild;
    const text =  node.textContent;
    expect(text.trim()).toBe('Hello World!');
  });
});
