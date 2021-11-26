# NetPad

## Start dev web server

    npm start

## Build the app in production mode

    npm run build

It builds all files to dist folder. To deploy to production server, copy all the `dist/*` files to production root folder.

For example
```
dist/index.html
dist/foo.12345.js
```
Copy to production root folder
```
root_folder/index.html
root_folder/foo.12345.js
```

## Unit Tests

    npm run test

Run unit tests in watch mode.

    npm run test:watch


## Analyze webpack bundle

    npm run analyze

## Cypress e2e test

All e2e tests are in `cypress/integration/`.

Run e2e tests with:

    npm run test:e2e

Note the `test:e2e` script uses start-server-and-test to boot up dev server on port 9000 first, then run cypress test, it will automatically shutdown the dev server after test was finished.

To run Cypress interactively, do

```bash
# Start the dev server in one terminal
npm start
# Start Cypress in another terminal
npx cypress open
```

For more information, visit https://www.cypress.io
