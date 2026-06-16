import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UiFieldBuilder } from './ui-field-builder';

describe('UiFieldBuilder', () => {
  let component: UiFieldBuilder;
  let fixture: ComponentFixture<UiFieldBuilder>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UiFieldBuilder],
    }).compileComponents();

    fixture = TestBed.createComponent(UiFieldBuilder);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
