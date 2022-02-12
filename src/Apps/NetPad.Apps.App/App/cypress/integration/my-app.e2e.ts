/// <reference types="Cypress" />

context('my-app', () => {
  it('shows message', () => {
    cy.visit('/');
    cy.get('my-app>div').contains('Hello World!');
  });
});
