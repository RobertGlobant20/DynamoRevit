## 상세
`Element.Solids`는 지정된 요소에 대한 솔리드 형상을 반환합니다. 솔리드는 중첩된 리스트로 반환되며, 지정된 요소는 둘 이상의 솔리드를 가질 수 있습니다. 요소를 나타내는 단일 솔리드가 필요한 경우 리스트에서 `Solid.ByUnion`을 사용할 수 있습니다.

아래 예에서는 모든 벽이 선택된 뷰에서 수집됩니다. 그런 다음 내부 패밀리로 작성된 벽이 제거되고 나머지 벽의 솔리드가 반환됩니다.

___
## 예제 파일

![Element.Solids](./Revit.Elements.Element.Solids_img.jpg)
