import {render} from './helper';
import {Window} from '../src/windows/main/window';

describe('my-app', () => {
  it('should render message', async () => {
    const node = (await render('<my-app></my-app>', Window)).firstElementChild;
    const text =  node.textContent;
    expect(text.trim()).toBe('Hello World!');
  });
});
