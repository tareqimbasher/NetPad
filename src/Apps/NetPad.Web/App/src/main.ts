import Aurelia from 'aurelia';
import "bootstrap";
import './styles/main.scss';
import { Index } from './main-window';

Aurelia
  .app({
      host: document.getElementsByTagName("main-window")[0],
      component: Index
  })
  .start();
